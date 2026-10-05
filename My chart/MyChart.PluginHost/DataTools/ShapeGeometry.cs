using MyChart.Core.Analysis;
using MyChart.Core.Contracts.Plugins;
using MyChart.Core.Models.Geometry;

namespace MyChart.PluginHost.DataTools;

public enum LineExtend
{
    None,
    Right,
    Both
}

/// <summary>
/// PG2.03 ShapeGeometry — pure hit/geometry in screen space (DIP).
/// AT15 vectors: Line, Rect, Fibonacci prices.
/// </summary>
public static class ShapeGeometry
{
    public static bool HitLine(PointD p, PointD a, PointD b, double toleranceDip, LineExtend extend = LineExtend.None)
    {
        double d = extend switch
        {
            LineExtend.Both => DrawingMath.DistancePointToLine(p, a, b),
            LineExtend.Right => DrawingMath.DistancePointToRay(p, a, b),
            _ => DrawingMath.DistancePointToSegment(p, a, b)
        };
        return d <= toleranceDip;
    }

    public static bool HitRect(PointD p, PointD p1, PointD p2, double toleranceDip)
    {
        double left = Math.Min(p1.X, p2.X);
        double top = Math.Min(p1.Y, p2.Y);
        double w = Math.Abs(p2.X - p1.X);
        double h = Math.Abs(p2.Y - p1.Y);
        var r = new RectD(left, top, w, h);
        if (DrawingMath.PointInRect(p, r))
            return true;
        return DrawingMath.DistancePointToRectEdges(p, r) <= toleranceDip;
    }

    public static bool HitEllipse(PointD p, PointD p1, PointD p2, double toleranceDip)
    {
        double cx = (p1.X + p2.X) / 2;
        double cy = (p1.Y + p2.Y) / 2;
        double rx = Math.Abs(p2.X - p1.X) / 2;
        double ry = Math.Abs(p2.Y - p1.Y) / 2;
        var c = new PointD(cx, cy);
        if (DrawingMath.PointInEllipse(p, c, rx, ry))
            return true;
        return DrawingMath.DistancePointToEllipseEdge(p, c, rx, ry) <= toleranceDip;
    }

    /// <summary>
    /// Fibonacci level prices (P1=1.1000, P2=1.1100): 0→1.11000, 0.236→1.10764, ...
    /// Formula: price = P2 + (P1 - P2) * level
    /// </summary>
    public static double[] FibonacciPrices(double p1, double p2, IReadOnlyList<double> levels)
    {
        var result = new double[levels.Count];
        for (int i = 0; i < levels.Count; i++)
            result[i] = DrawingMath.FibonacciPrice(p1, p2, levels[i]);
        return result;
    }

    public static readonly double[] StandardFibonacciLevels =
        { 0, 0.236, 0.382, 0.5, 0.618, 0.786, 1 };
}
