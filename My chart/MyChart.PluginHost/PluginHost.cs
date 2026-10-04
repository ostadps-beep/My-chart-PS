using MyChart.Core.Contracts.Plugins;

namespace MyChart.PluginHost;

/// <summary>Runtime host that components register into. Implementation filled in later PG nodes.</summary>
public sealed class PluginHostService : IPluginHost
{
    public IToolRegistry Tools { get; } = null!;
    public IDrawingTypeRegistry DrawingTypes { get; } = null!;
    public IIconRegistry Icons { get; } = null!;
    public IContributionRegistry Contributions { get; } = null!;
}
