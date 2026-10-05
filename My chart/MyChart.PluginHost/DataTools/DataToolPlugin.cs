using MyChart.Core.Contracts.Plugins;
using MyChart.Core.Models.Plugins;
using MyChart.Core.Plugins.Vocabulary;

namespace MyChart.PluginHost.DataTools;

/// <summary>
/// PG2.05 DataToolPlugin — registers tool + painter + hit tester from a validated definition.
/// </summary>
public sealed class DataToolPlugin : IChartPlugin
{
    private readonly string _componentId;
    private readonly string _toolId;
    private readonly string _typeId;
    private readonly string _iconKey;
    private readonly ToolDefinition _definition;
    private readonly string _shapeKind;

    public DataToolPlugin(
        string componentId,
        string toolId,
        string typeId,
        string iconKey,
        ToolDefinition definition,
        string shapeKind = "Line")
    {
        _componentId = componentId;
        _toolId = toolId;
        _typeId = typeId;
        _iconKey = iconKey;
        _definition = definition;
        _shapeKind = shapeKind;
    }

    public string ComponentId => _componentId;

    public void Register(IPluginHost host)
    {
        host.Tools.Register(
            new ToolDescriptor(
                _componentId,
                _toolId,
                _toolId,
                "Draw",
                _iconKey,
                null,
                _typeId,
                _definition.Anchors,
                Array.Empty<ParameterDescriptor>()),
            ctx =>
            {
                var tool = new DataToolFactory(_toolId, _typeId, _definition.Anchors, _definition.Workflow);
                tool.Bind(ctx);
                return tool;
            });

        host.DrawingTypes.Register(
            new DrawingTypeDescriptor(_typeId, 1, _definition.Anchors),
            new DataToolPainter(_shapeKind),
            new DataToolHitTester(_shapeKind),
            migrator: null);
    }
}
