using MyChart.Core.Contracts.Plugins;
using MyChart.Core.Models.Drawing;

namespace MyChart.PluginHost.Guard;

public sealed class GuardedPainter : IDrawingObjectPainter
{
    private readonly IDrawingObjectPainter _inner;
    private readonly CircuitBreaker _breaker;

    public GuardedPainter(IDrawingObjectPainter inner, CircuitBreaker breaker)
    {
        _inner = inner;
        _breaker = breaker;
    }

    public void Paint(DrawContext ctx, DrawingObject obj)
    {
        if (_breaker.IsOpen) return;
        try
        {
            _inner.Paint(ctx, obj);
        }
        catch
        {
            _breaker.RecordFault();
        }
    }
}
