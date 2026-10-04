using MyChart.Core.Models.Geometry;

namespace MyChart.Core.Analysis;

/// <summary>
/// Pure geometry helpers for hit-testing. No IO, no pixels conversion beyond coordinates given.
/// </summary>
public static class DrawingMath
{
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

    public static double DistancePointToRectEdges(PointD p, RectD r)
    {
        // Distance to nearest edge of the rectangle
        double dx = Math.Max(Math.Max(r.X - p.X, 0), p.X - r.Right);
        double dy = Math.Max(Math.Max(r.Y - p.Y, 0), p.Y - r.Bottom);
        if (dx > 0 || dy > 0)
            return Math.Sqrt(dx * dx + dy * dy);

        // Inside: distance to nearest edge
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
        // Approximate distance to edge in the same units
        return Math.Abs(dist - 1.0) * Math.Min(radiusX, radiusY);
    }
}
