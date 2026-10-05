using MyChart.Core.Models.Geometry;

namespace MyChart.Core.Analysis;

/// <summary>
/// Pure geometry helpers for hit-testing. No IO, no pixels conversion beyond coordinates given.
/// </summary>
public static class DrawingMath
{
    public const int EllipseSegments = 48;

    public static double DistancePointToSegment(PointD p, PointD a, PointD b)
    {
        double dx = b.X - a.X;
        double dy = b.Y - a.Y;
        if (dx == 0 && dy == 0)
            return Math.Sqrt((p.X - a.X) * (p.X - a.X) + (p.Y - a.Y) * (p.Y - a.Y));

        double t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / (dx * dx + dy * dy);
        t = Math.Clamp(t, 0.0, 1.0);
        double projX = a.X + t * dx;
        double projY = a.Y + t * dy;
        return Math.Sqrt((p.X - projX) * (p.X - projX) + (p.Y - projY) * (p.Y - projY));
    }

    /// <summary>Distance to infinite line through a and b.</summary>
    public static double DistancePointToLine(PointD p, PointD a, PointD b)
    {
        double dx = b.X - a.X;
        double dy = b.Y - a.Y;
        if (dx == 0 && dy == 0)
            return Math.Sqrt((p.X - a.X) * (p.X - a.X) + (p.Y - a.Y) * (p.Y - a.Y));
        return Math.Abs(dy * p.X - dx * p.Y + b.X * a.Y - b.Y * a.X) / Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>Distance to ray starting at a going through b (t >= 0).</summary>
    public static double DistancePointToRay(PointD p, PointD a, PointD b)
    {
        double dx = b.X - a.X;
        double dy = b.Y - a.Y;
        if (dx == 0 && dy == 0)
            return Math.Sqrt((p.X - a.X) * (p.X - a.X) + (p.Y - a.Y) * (p.Y - a.Y));
        double t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / (dx * dx + dy * dy);
        if (t < 0) t = 0;
        double projX = a.X + t * dx;
        double projY = a.Y + t * dy;
        return Math.Sqrt((p.X - projX) * (p.X - projX) + (p.Y - projY) * (p.Y - projY));
    }

    public static double DistancePointToRectEdges(PointD p, RectD r)
    {
        double dx = Math.Max(Math.Max(r.X - p.X, 0), p.X - r.Right);
        double dy = Math.Max(Math.Max(r.Y - p.Y, 0), p.Y - r.Bottom);
        if (dx > 0 || dy > 0)
            return Math.Sqrt(dx * dx + dy * dy);

        double left = p.X - r.X;
        double right = r.Right - p.X;
        double top = p.Y - r.Y;
        double bottom = r.Bottom - p.Y;
        return Math.Min(Math.Min(left, right), Math.Min(top, bottom));
    }

    public static bool PointInRect(PointD p, RectD r) => r.Contains(p);

    public static double DistancePointToEllipseEdge(PointD p, PointD center, double radiusX, double radiusY)
    {
        if (radiusX <= 0 || radiusY <= 0) return double.PositiveInfinity;
        double nx = (p.X - center.X) / radiusX;
        double ny = (p.Y - center.Y) / radiusY;
        double dist = Math.Sqrt(nx * nx + ny * ny);
        return Math.Abs(dist - 1.0) * Math.Min(radiusX, radiusY);
    }

    public static bool PointInEllipse(PointD p, PointD center, double radiusX, double radiusY)
    {
        if (radiusX <= 0 || radiusY <= 0) return false;
        double nx = (p.X - center.X) / radiusX;
        double ny = (p.Y - center.Y) / radiusY;
        return nx * nx + ny * ny <= 1.0;
    }

    /// <summary>Fibonacci prices for levels between P1 and P2 (AT15 / GOLDEN DrawingHit).</summary>
    public static double FibonacciPrice(double p1, double p2, double level)
        => p2 + (p1 - p2) * level;
}
