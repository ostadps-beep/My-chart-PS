using MyChart.Core.Plugins.Identity;
using MyChart.Core.Plugins.Manifest;

namespace MyChart.Generator.Engine.Operations;

/// <summary>
/// PG3.07 ToolOperations — add / update / remove / enable / disable / list / regenerate.
/// Each mutating verb is one Transaction (PG3.04).
/// </summary>
public sealed class ToolOperations
{
    private readonly PathScope _scope;
    private readonly string _repoComponentsRoot;
    private readonly string _userComponentsRoot;

    public ToolOperations(PathScope scope, string? repoComponentsRoot = null, string? userComponentsRoot = null)
    {
        _scope = scope;
        _repoComponentsRoot = repoComponentsRoot
            ?? Path.Combine(scope.RepoRoot, "MyChart.Plugins", "Components");
        _userComponentsRoot = userComponentsRoot
            ?? Path.Combine(scope.UserRoot);
    }

    public TransactionResult Add(ToolAddRequest req)
    {
        // E023 CodeTool in UserRoot
        if (req.Kind == ToolKind.CodeTool && req.Target == ComponentTarget.User)
        {
            return new TransactionResult
            {
                Ok = false,
                ErrorCode = ErrorCodes.CodeToolInUserRoot,
                ErrorMessage = "CodeTool not allowed in UserRoot",
                ExitCode = 1
            };
        }

        var idErr = IdRules.Validate(req.ComponentId);
        if (idErr is not null)
        {
            return new TransactionResult
            {
                Ok = false,
                ErrorCode = idErr,
                ErrorMessage = "Invalid ComponentId",
                ExitCode = 1
            };
        }

        var root = req.Target == ComponentTarget.Repo ? _repoComponentsRoot : _userComponentsRoot;
        var folder = Path.Combine(root, req.ComponentId);

        // E002 duplicate folder
        if (Directory.Exists(folder))
        {
            return new TransactionResult
            {
                Ok = false,
                ErrorCode = ErrorCodes.DuplicateId,
                ErrorMessage = req.ComponentId,
                ExitCode = 1
            };
        }

        var kind = req.Kind == ToolKind.CodeTool
            ? ScaffoldKind.CodeToolClick
            : req.ClickThenText
                ? ScaffoldKind.DataToolClickThenText
                : ScaffoldKind.DataToolClick;

        var files = Scaffolder.Build(new ScaffoldRequest
        {
            Kind = kind,
            ComponentId = req.ComponentId,
            Name = req.Name,
            TypeId = req.TypeId,
            IconKey = req.IconKey,
            ToolId = req.ToolId,
            Anchors = req.Anchors
        });

        var plan = new FilePlan();
        foreach (var (rel, content) in files)
            plan.AddCreate(Path.Combine(folder, rel), content);

        // regenerate catalogs into plan when targeting repo
        if (req.Target == ComponentTarget.Repo)
            AppendCatalogRegeneration(plan, extra: files, componentId: req.ComponentId, folder: folder);

        var tx = new Transaction(_scope);
        return tx.Execute(
            plan,
            dryRun: req.DryRun,
            allowDirty: req.AllowDirty,
            requireGitForRepoPaths: req.Target == ComponentTarget.Repo);
    }

    public TransactionResult Remove(string componentId, ComponentTarget target, bool dryRun = false, bool allowDirty = false)
    {
        var root = target == ComponentTarget.Repo ? _repoComponentsRoot : _userComponentsRoot;
        var folder = Path.Combine(root, componentId);
        if (!Directory.Exists(folder))
        {
            return new TransactionResult
            {
                Ok = false,
                ErrorCode = ErrorCodes.DependencyMissing,
                ErrorMessage = "component not found: " + componentId,
                ExitCode = 2
            };
        }

        var plan = new FilePlan();
        foreach (var file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
            plan.AddDelete(file);

        if (target == ComponentTarget.Repo)
            AppendCatalogRegeneration(plan, removedId: componentId);

        var tx = new Transaction(_scope);
        var result = tx.Execute(
            plan,
            dryRun: dryRun,
            allowDirty: allowDirty,
            requireGitForRepoPaths: target == ComponentTarget.Repo);

        // after successful commit, remove empty directories
        if (result.Ok && !dryRun)
        {
            try { Directory.Delete(folder, recursive: true); } catch { /* best effort */ }
        }

        return result;
    }

    public TransactionResult SetActive(string componentId, ComponentTarget target, bool active, bool dryRun = false, bool allowDirty = false)
    {
        var root = target == ComponentTarget.Repo ? _repoComponentsRoot : _userComponentsRoot;
        var manifestPath = Path.Combine(root, componentId, "Manifest.json");
        if (!File.Exists(manifestPath))
        {
            return new TransactionResult
            {
                Ok = false,
                ErrorCode = ErrorCodes.DependencyMissing,
                ErrorMessage = componentId,
                ExitCode = 2
            };
        }

        var manifest = ManifestIO.ReadFile(manifestPath);
        manifest.Active = active;
        var json = ManifestIO.Write(manifest);

        var plan = new FilePlan();
        plan.AddOverwrite(manifestPath, json, ManifestIO.Sha256File(manifestPath));

        if (target == ComponentTarget.Repo)
            AppendCatalogRegeneration(plan);

        var tx = new Transaction(_scope);
        return tx.Execute(plan, dryRun, allowDirty, requireGitForRepoPaths: target == ComponentTarget.Repo);
    }

    public IReadOnlyList<ComponentManifest> List(ComponentTarget? target = null)
    {
        var roots = new List<string>();
        if (target is null or ComponentTarget.Repo) roots.Add(_repoComponentsRoot);
        if (target is null or ComponentTarget.User) roots.Add(_userComponentsRoot);

        var list = new List<ComponentManifest>();
        foreach (var root in roots)
        {
            if (!Directory.Exists(root)) continue;
            foreach (var dir in Directory.EnumerateDirectories(root))
            {
                var mp = Path.Combine(dir, "Manifest.json");
                if (!File.Exists(mp)) continue;
                try { list.Add(ManifestIO.ReadFile(mp)); }
                catch { /* skip broken */ }
            }
        }

        return list.OrderBy(m => m.ComponentId, StringComparer.Ordinal).ToList();
    }

    public TransactionResult RegenerateCatalogs(bool dryRun = false, bool allowDirty = false)
    {
        var plan = new FilePlan();
        AppendCatalogRegeneration(plan);
        var tx = new Transaction(_scope);
        return tx.Execute(plan, dryRun, allowDirty, requireGitForRepoPaths: true);
    }

    private void AppendCatalogRegeneration(
        FilePlan plan,
        IReadOnlyDictionary<string, string>? extra = null,
        string? componentId = null,
        string? folder = null,
        string? removedId = null)
    {
        // Build component inputs from disk + optional in-memory extra for a new add
        var inputs = CatalogGenerator.ScanRepoComponents(_repoComponentsRoot);

        if (removedId is not null)
            inputs.RemoveAll(c => c.ComponentId == removedId);

        if (extra is not null && componentId is not null && folder is not null)
        {
            // synthetic input for the component being added (not yet on disk)
            if (extra.TryGetValue("Manifest.json", out var man))
            {
                ComponentManifest? parsed = null;
                try { parsed = ManifestIO.Read(man); } catch { /* ignore */ }
                inputs.RemoveAll(c => c.ComponentId == componentId);
                inputs.Add(new CatalogGenerator.ComponentInput
                {
                    ComponentId = componentId,
                    Kind = parsed?.Kind ?? "DataTool",
                    Active = parsed?.Active ?? true,
                    ManifestJson = man,
                    DefinitionJson = extra.TryGetValue("Definition.json", out var d) ? d : null,
                    IconKey = parsed?.IconKey,
                    GeometryPathData = extra.TryGetValue("Geometry.txt", out var g) ? g.Trim() : null
                });
            }
        }

        var (pluginCs, iconCs, _) = CatalogGenerator.Generate(inputs);

        var pluginPath = _scope.PluginCatalogPath;
        var iconPath = _scope.IconCatalogPath;

        if (File.Exists(pluginPath))
            plan.AddOverwrite(pluginPath, pluginCs, ManifestIO.Sha256File(pluginPath));
        else
            plan.AddCreate(pluginPath, pluginCs);

        if (File.Exists(iconPath))
            plan.AddOverwrite(iconPath, iconCs, ManifestIO.Sha256File(iconPath));
        else
            plan.AddCreate(iconPath, iconCs);
    }
}
