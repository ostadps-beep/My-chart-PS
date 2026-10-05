using MyChart.Core.Commands;
using MyChart.Core.Contracts.Plugins;
using MyChart.PluginHost.Registries;

namespace MyChart.PluginHost.Loading;

/// <summary>
/// PG4.01 CompositionRoot wiring — PluginHost + CatalogSource + UserFolderSource.
/// </summary>
public sealed class ChartComposition
{
    public PluginHostService Host { get; }
    public DrawingList Drawings { get; } = new();
    public CommandBus Commands { get; } = new();
    public PluginLoadReport LoadReport { get; private set; } = new();

    public ChartComposition()
    {
        Host = new PluginHostService();
    }

    public PluginLoadReport Load(params IComponentSource[] sources)
    {
        LoadReport = Host.Load(sources);
        return LoadReport;
    }

    public IDrawingObjectPainter? ResolvePainter(string typeId)
    {
        if (Host.DrawingTypes.TryGet(typeId, out _, out var painter, out _, out _))
            return painter;
        return null;
    }

    public IDrawingHitTester? ResolveHitTester(string typeId)
    {
        if (Host.DrawingTypes.TryGet(typeId, out _, out _, out var hit, out _))
            return hit;
        return null;
    }

    public Func<IToolContext, IDrawingTool>? ResolveToolFactory(string toolId)
    {
        if (Host.Tools.TryGet(toolId, out _, out var factory))
            return factory;
        return null;
    }
}
