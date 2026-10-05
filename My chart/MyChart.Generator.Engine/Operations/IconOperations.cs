using MyChart.Core.Plugins.Identity;
using MyChart.Core.Plugins.Manifest;

namespace MyChart.Generator.Engine.Operations;

/// <summary>PG3.07 IconOperations — icon add / remove.</summary>
public sealed class IconOperations
{
    private readonly PathScope _scope;
    private readonly ToolOperations _tools;
    private readonly string _repoComponentsRoot;
    private readonly string _userComponentsRoot;

    public IconOperations(PathScope scope, string? repoComponentsRoot = null, string? userComponentsRoot = null)
    {
        _scope = scope;
        _repoComponentsRoot = repoComponentsRoot
            ?? Path.Combine(scope.RepoRoot, "MyChart.Plugins", "Components");
        _userComponentsRoot = userComponentsRoot ?? scope.UserRoot;
        _tools = new ToolOperations(scope, _repoComponentsRoot, _userComponentsRoot);
    }

    public TransactionResult Add(IconAddRequest req)
    {
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

        var geoErr = GeometryPathGrammar.Validate(req.GeometryPathData);
        if (geoErr is not null)
        {
            return new TransactionResult
            {
                Ok = false,
                ErrorCode = geoErr,
                ErrorMessage = "Invalid geometry",
                ExitCode = 1
            };
        }

        var root = req.Target == ComponentTarget.Repo ? _repoComponentsRoot : _userComponentsRoot;
        var folder = Path.Combine(root, req.ComponentId);
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

        var files = Scaffolder.Build(new ScaffoldRequest
        {
            Kind = ScaffoldKind.IconVector,
            ComponentId = req.ComponentId,
            Name = req.Name,
            IconKey = req.IconKey
        });
        // override geometry with request value
        files = new Dictionary<string, string>(files, StringComparer.Ordinal)
        {
            ["Geometry.txt"] = req.GeometryPathData.TrimEnd() + "\n"
        };

        var plan = new FilePlan();
        foreach (var (rel, content) in files)
            plan.AddCreate(Path.Combine(folder, rel), content);

        // reuse tool ops catalog regeneration via a temporary ToolOperations path
        // simplest: write via transaction then call regenerate separately for repo
        var tx = new Transaction(_scope);
        var result = tx.Execute(
            plan,
            dryRun: req.DryRun,
            allowDirty: req.AllowDirty,
            requireGitForRepoPaths: req.Target == ComponentTarget.Repo);

        if (result.Ok && !req.DryRun && req.Target == ComponentTarget.Repo)
            return _tools.RegenerateCatalogs(dryRun: false, allowDirty: true);

        return result;
    }

    public TransactionResult Remove(string componentId, ComponentTarget target, bool dryRun = false, bool allowDirty = false)
        => _tools.Remove(componentId, target, dryRun, allowDirty);
}
