using MyChart.Core.Commands;
using MyChart.Core.Contracts.Commands;
using MyChart.Core.Contracts.Plugins;
using MyChart.Core.Models.Drawing;

namespace MyChart.Core.Analysis;

/// <summary>
/// PG4.01 tool activation — factory by ToolId; Commit → AddDrawingCommand.
/// </summary>
public sealed class ToolSession
{
    private readonly ICommandBus _bus;
    private readonly DrawingList _list;
    private readonly Func<string, Func<IToolContext, IDrawingTool>?> _resolveFactory;
    private IDrawingTool? _active;

    public ToolSession(
        ICommandBus bus,
        DrawingList list,
        Func<string, Func<IToolContext, IDrawingTool>?> resolveFactory)
    {
        _bus = bus;
        _list = list;
        _resolveFactory = resolveFactory;
    }

    public string? ActiveToolId { get; private set; }
    public DrawingObject? Preview => _active?.Preview;

    public bool Activate(string toolId, IToolContext ctx)
    {
        Deactivate();
        var factory = _resolveFactory(toolId);
        if (factory is null) return false;
        _active = factory(ctx);
        _active.Activate();
        ActiveToolId = toolId;
        return true;
    }

    public void Deactivate()
    {
        _active?.Deactivate();
        _active = null;
        ActiveToolId = null;
    }

    public ToolResult OnPointer(PointerEvent e)
    {
        if (_active is null) return new ToolResult.None();
        var result = _active.OnPointer(e);
        if (result is ToolResult.Commit commit)
        {
            _bus.Execute(new AddDrawingCommand(_list, commit.Object));
            _active.Deactivate();
            // keep tool active for next object of same type
            _active.Activate();
        }
        else if (result is ToolResult.Cancel)
        {
            _active.Deactivate();
            _active.Activate();
        }
        return result;
    }

    public ToolResult OnKey(KeyEvent e)
    {
        if (_active is null) return new ToolResult.None();
        return _active.OnKey(e);
    }
}
