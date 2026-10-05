using MyChart.Core.Contracts.Plugins;
using MyChart.Core.Models.Drawing;
using MyChart.Core.Models.Geometry;

namespace MyChart.PluginHost.Guard;

public sealed class GuardedHitTester : IDrawingHitTester
{
    private readonly IDrawingHitTester _inner;
    private readonly CircuitBreaker _breaker;

    public GuardedHitTester(IDrawingHitTester inner, CircuitBreaker breaker)
    {
        _inner = inner;
        _breaker = breaker;
    }

    public HitResult HitTest(DrawingObject obj, PointD p, IChartMapper map, double toleranceDip)
    {
        if (_breaker.IsOpen)
            return new HitResult(HitKind.None);
        try
        {
            return _inner.HitTest(obj, p, map, toleranceDip);
        }
        catch
        {
            _breaker.RecordFault();
            return new HitResult(HitKind.None);
        }
    }
}
