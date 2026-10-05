using MyChart.Core.Plugins.Manifest;

namespace MyChart.Generator.Engine.Operations;

/// <summary>
/// PG3.06 Validate — read-only scan of both roots; writes nothing.
/// </summary>
public static class Validate
{
    public sealed class ComponentRecord
    {
        public required string ComponentId { get; init; }
        public required string Kind { get; init; }
        public required string RootPath { get; init; }
        public required string ManifestPath { get; init; }
        public required ComponentManifest Manifest { get; init; }
        public string? DefinitionPath { get; init; }
        public string? GeometryPath { get; init; }
        public string? ExpectedGeneratedSha { get; init; }
        public string? ActualGeneratedSha { get; init; }
    }

    public static ValidateReport Run(
        IReadOnlyList<ComponentRecord> components,
        string? pluginCatalogPath = null,
        string? expectedCatalogHash = null,
        string? actualCatalogHash = null,
        IReadOnlyDictionary<string, string>? recordedGeneratedShas = null)
    {
        var report = new ValidateReport();
        var graph = new DependencyGraph();

        var byId = new Dictionary<string, ComponentRecord>(StringComparer.Ordinal);
        var toolIds = new Dictionary<string, string>(StringComparer.Ordinal);
        var typeIds = new Dictionary<string, string>(StringComparer.Ordinal);
        var iconKeys = new Dictionary<string, string>(StringComparer.Ordinal);
        var hotkeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var c in components)
        {
            graph.AddNode(c.ComponentId);

            if (!File.Exists(c.ManifestPath))
                report.MissingFiles.Add(c.ManifestPath);

            if (c.Kind == "DataTool")
            {
                if (c.DefinitionPath is null || !File.Exists(c.DefinitionPath))
                    report.MissingFiles.Add(c.DefinitionPath ?? c.ComponentId + "/Definition.json");
            }

            if (c.Kind == "Icon")
            {
                if (c.GeometryPath is null || !File.Exists(c.GeometryPath))
                    report.MissingFiles.Add(c.GeometryPath ?? c.ComponentId + "/Geometry.txt");
            }

            // duplicates
            if (!byId.TryAdd(c.ComponentId, c))
            {
                report.Duplicates.Add("ComponentId:" + c.ComponentId);
                report.Errors.Add((ErrorCodes.DuplicateId, c.ComponentId));
            }

            if (c.Manifest.ToolId is { } tid)
            {
                if (!toolIds.TryAdd(tid, c.ComponentId))
                {
                    report.Duplicates.Add("ToolId:" + tid);
                    report.Errors.Add((ErrorCodes.DuplicateToolId, tid));
                }
            }

            if (c.Manifest.TypeId is { } ty)
            {
                if (!typeIds.TryAdd(ty, c.ComponentId))
                {
                    report.Duplicates.Add("TypeId:" + ty);
                    report.Errors.Add((ErrorCodes.DuplicateTypeId, ty));
                }
            }

            if (c.Manifest.IconKey is { } ik)
            {
                if (c.Kind == "Icon")
                {
                    if (!iconKeys.TryAdd(ik, c.ComponentId))
                    {
                        report.Duplicates.Add("IconKey:" + ik);
                        report.Errors.Add((ErrorCodes.DuplicateId, ik));
                    }
                }
            }

            if (c.Manifest.Hotkey is { } hk && hk.Length > 0)
            {
                if (!hotkeys.TryAdd(hk, c.ComponentId))
                {
                    report.Duplicates.Add("Hotkey:" + hk);
                    report.Errors.Add((ErrorCodes.HotkeyConflict, hk));
                }
            }

            // tool -> icon dependency
            if (c.Kind is "DataTool" or "CodeTool" && c.Manifest.IconKey is { } iconKey)
            {
                // dependency edge uses icon component id if known, else the key as placeholder node
                var iconNode = iconKeys.TryGetValue(iconKey, out var iconId) ? iconId : iconKey;
                graph.AddEdge(c.ComponentId, iconNode);
            }

            // hand-edited generated
            if (recordedGeneratedShas is not null
                && recordedGeneratedShas.TryGetValue(c.ManifestPath, out var expected)
                && c.ActualGeneratedSha is not null
                && !string.Equals(expected, c.ActualGeneratedSha, StringComparison.Ordinal))
            {
                report.HandEditedGenerated.Add(c.ManifestPath);
            }
        }

        // missing icon targets
        foreach (var (from, to) in graph.MissingTargets())
        {
            report.BrokenReferences.Add(from + "->" + to);
            report.Errors.Add((ErrorCodes.DependencyMissing, from + " depends on missing " + to));
        }

        // also: tool iconKey not found among Icon components
        var knownIconKeys = new HashSet<string>(
            components.Where(x => x.Kind == "Icon" && x.Manifest.IconKey is not null)
                .Select(x => x.Manifest.IconKey!),
            StringComparer.Ordinal);

        foreach (var c in components.Where(x => x.Kind is "DataTool" or "CodeTool"))
        {
            if (c.Manifest.IconKey is { } ik && !knownIconKeys.Contains(ik))
            {
                report.BrokenReferences.Add(c.ComponentId + " icon:" + ik);
                report.Errors.Add((ErrorCodes.MissingIcon, ik));
            }
        }

        var cycle = graph.FindCycle();
        if (cycle is not null)
            report.Errors.Add((ErrorCodes.DependencyCycle, string.Join(" -> ", cycle)));

        if (expectedCatalogHash is not null
            && actualCatalogHash is not null
            && !string.Equals(expectedCatalogHash, actualCatalogHash, StringComparison.Ordinal))
        {
            report.CatalogStale = true;
        }

        if (pluginCatalogPath is not null && !File.Exists(pluginCatalogPath))
            report.MissingFiles.Add(pluginCatalogPath);

        return report;
    }

    /// <summary>Check remove/disable of id would hit E012.</summary>
    public static List<string> CheckHasDependents(DependencyGraph graph, string id)
        => graph.HasDependents(id);
}
