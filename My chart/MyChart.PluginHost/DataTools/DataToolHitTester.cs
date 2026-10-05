using MyChart.Core.Analysis;
using MyChart.Core.Contracts.Plugins;
using MyChart.Core.Models.Drawing;
using MyChart.Core.Models.Geometry;

namespace MyChart.PluginHost.DataTools;

/// <summary>
/// Generic hit tester for simple 2-anchor Line/Rect objects (DataTool shapes).
/// Handles are evaluated by DrawingGeometry (core); this tests the body only.
/// </summary>
public sealed class DataToolHitTester : IDrawingHitTester
{
    private readonly string _shape; // "Line" or "Rect"

    public DataToolHitTester(string shape = "Line") => _shape = shape;

    public HitResult HitTest(DrawingObject obj, PointD p, IChartMapper map, double toleranceDip)
    {
        if (obj.Anchors.Count < 2)
            return new HitResult(HitKind.None);

        var a = Screen(obj.Anchors[0], map);
        var b = Screen(obj.Anchors[1], map);

        bool hit = _shape switch
        {
            "Rect" => ShapeGeometry.HitRect(p, a, b, toleranceDip),
            "Ellipse" => ShapeGeometry.HitEllipse(p, a, b, toleranceDip),
            _ => ShapeGeometry.HitLine(p, a, b, toleranceDip)
        };

        return hit ? new HitResult(HitKind.Body) : new HitResult(HitKind.None);
    }

    private static PointD Screen(DrawingAnchor anchor, IChartMapper map)
    {
        double u = map.IndexOfTime(anchor.TimeUtc);
        return new PointD(map.X(u), map.Y(anchor.Price));
    }
}
