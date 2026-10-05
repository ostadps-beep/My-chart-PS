using System.Text.Json.Nodes;
using MyChart.Core.Models.Plugins;
using MyChart.Core.Plugins.Identity;
using MyChart.Core.Plugins.Manifest;
using MyChart.Core.Plugins.Vocabulary;
using MyChart.PluginHost.DataTools;

namespace MyChart.PluginHost.Loading;

/// <summary>
/// PG2.06 UserComponentLoader — scan UserRoot/*/Manifest.json; DataTool and Icon only.
/// </summary>
public sealed class UserComponentLoader : IComponentSource
{
    private readonly string _userRoot;
    private readonly HashSet<string> _repoComponentIds;

    public UserComponentLoader(string userRoot, IEnumerable<string>? repoComponentIds = null)
    {
        _userRoot = userRoot;
        _repoComponentIds = new HashSet<string>(repoComponentIds ?? Array.Empty<string>(), StringComparer.Ordinal);
    }

    public IEnumerable<ComponentSet> Load(PluginLoadReport report)
    {
        if (!Directory.Exists(_userRoot))
            yield break; // missing folder is not an error

        foreach (var dir in Directory.EnumerateDirectories(_userRoot))
        {
            var manifestPath = Path.Combine(dir, "Manifest.json");
            if (!File.Exists(manifestPath))
                continue;

            ComponentSet? set = null;
            try
            {
                set = TryLoadOne(dir, manifestPath, report);
            }
            catch (Exception ex)
            {
                report.AddFailure(Path.GetFileName(dir), ErrorCodes.DefinitionSchemaError, ex.Message);
            }

            if (set is not null)
                yield return set;
        }
    }

    private ComponentSet? TryLoadOne(string dir, string manifestPath, PluginLoadReport report)
    {
        var json = File.ReadAllText(manifestPath);
        var root = JsonNode.Parse(json) as JsonObject
            ?? throw new InvalidOperationException("Invalid manifest JSON");

        var componentId = root["componentId"]?.GetValue<string>()
            ?? root["id"]?.GetValue<string>()
            ?? Path.GetFileName(dir);
        var kind = root["kind"]?.GetValue<string>() ?? "DataTool";

        if (kind == "CodeTool")
        {
            report.AddFailure(componentId, ErrorCodes.CodeToolInUserRoot, "CodeTool not allowed in UserRoot");
            return null;
        }

        if (_repoComponentIds.Contains(componentId))
        {
            report.AddFailure(componentId, ErrorCodes.DuplicateId, "Duplicates RepoRoot component");
            return null;
        }

        if (kind == "Icon")
        {
            var iconKey = root["iconKey"]?.GetValue<string>() ?? $"Icon.{componentId}";
            var geoPath = Path.Combine(dir, "Geometry.txt");
            var pathData = File.Exists(geoPath) ? File.ReadAllText(geoPath).Trim() : "M0 0 L16 16";
            var geoErr = GeometryPathGrammar.Validate(pathData);
            if (geoErr is not null)
            {
                report.AddFailure(componentId, geoErr, "Invalid Geometry.txt");
                return null;
            }
            return new ComponentSet
            {
                ComponentId = componentId,
                Plugin = new NoOpPlugin(componentId),
                Icons = new[] { new IconDescriptor(iconKey, pathData) }
            };
        }

        // DataTool
        var defPath = Path.Combine(dir, "Definition.json");
        if (!File.Exists(defPath))
        {
            report.AddFailure(componentId, ErrorCodes.DefinitionSchemaError, "Missing Definition.json");
            return null;
        }

        var defJson = File.ReadAllText(defPath);
        var err = DefinitionValidator.ValidateJson(defJson, out var definition);
        if (err is not null || definition is null)
        {
            report.AddFailure(componentId, err ?? ErrorCodes.DefinitionSchemaError, "Invalid Definition.json");
            return null;
        }

        var toolId = root["toolId"]?.GetValue<string>() ?? componentId;
        var typeId = root["typeId"]?.GetValue<string>() ?? $"drawing.{componentId}";
        var iconKey2 = root["iconKey"]?.GetValue<string>() ?? "Icon.Default";
        var shape = definition.Shapes.FirstOrDefault()?["kind"]?.GetValue<string>() ?? "Line";

        return new ComponentSet
        {
            ComponentId = componentId,
            Plugin = new DataToolPlugin(componentId, toolId, typeId, iconKey2, definition, shape),
            Icons = Array.Empty<IconDescriptor>()
        };
    }

    private sealed class NoOpPlugin : Core.Contracts.Plugins.IChartPlugin
    {
        public NoOpPlugin(string id) => ComponentId = id;
        public string ComponentId { get; }
        public void Register(Core.Contracts.Plugins.IPluginHost host) { }
    }
}
