using MyChart.Core.Contracts.Plugins;
using MyChart.Core.Contracts.Services;
using MyChart.Core.Plugins.Identity;
using MyChart.Core.Plugins.Manifest;
using MyChart.PluginHost.Registries;

namespace MyChart.PluginHost.Loading;

/// <summary>
/// PG1.03 PluginHost: collect sources → icons → components (try/catch) → report.
/// </summary>
public sealed class PluginHostService : IPluginHost
{
    public ToolRegistry Tools { get; } = new();
    public DrawingTypeRegistry DrawingTypes { get; } = new();
    public IconRegistry Icons { get; } = new();
    public ContributionRegistry Contributions { get; } = new();

    IToolRegistry IPluginHost.Tools => Tools;
    IDrawingTypeRegistry IPluginHost.DrawingTypes => DrawingTypes;
    IIconRegistry IPluginHost.Icons => Icons;
    IContributionRegistry IPluginHost.Contributions => Contributions;

    public PluginLoadReport LastReport { get; private set; } = new();

    public PluginLoadReport Load(IEnumerable<IComponentSource> sources)
    {
        var report = new PluginLoadReport();
        var sets = new List<ComponentSet>();

        foreach (var source in sources)
        {
            try
            {
                sets.AddRange(source.Load(report));
            }
            catch (Exception ex)
            {
                report.AddFailure("(source)", ErrorCodes.InvalidId, ex.Message);
            }
        }

        // 1) icons first
        foreach (var set in sets.OrderBy(s => s.ComponentId, StringComparer.Ordinal))
        {
            foreach (var icon in set.Icons)
            {
                try
                {
                    var geo = GeometryPathGrammar.Validate(icon.PathData);
                    if (geo is not null)
                    {
                        report.AddFailure(set.ComponentId, geo, "Invalid icon PathData");
                        continue;
                    }
                    Icons.Register(icon);
                }
                catch (DuplicateRegistrationException)
                {
                    report.AddFailure(set.ComponentId, ErrorCodes.DuplicateId, $"Icon '{icon.IconKey}'");
                }
            }
        }

        // 2) components
        foreach (var set in sets.OrderBy(s => s.ComponentId, StringComparer.Ordinal))
        {
            try
            {
                set.Plugin.Register(this);
                report.Loaded.Add(set.ComponentId);
            }
            catch (DuplicateRegistrationException dex)
            {
                report.AddFailure(set.ComponentId, ErrorCodes.DuplicateToolId, dex.Message);
            }
            catch (Exception ex)
            {
                report.AddFailure(set.ComponentId, ErrorCodes.InvalidId, ex.Message);
            }
        }

        LastReport = report;
        return report;
    }
}
