namespace MyChart.PluginHost.Loading;

public interface IComponentSource
{
    IEnumerable<ComponentSet> Load(PluginLoadReport report);
}

/// <summary>One component (plugin + optional icons) from a source.</summary>
public sealed class ComponentSet
{
    public required string ComponentId { get; init; }
    public required Core.Contracts.Plugins.IChartPlugin Plugin { get; init; }
    public IReadOnlyList<Core.Models.Plugins.IconDescriptor> Icons { get; init; } = Array.Empty<Core.Models.Plugins.IconDescriptor>();
}
