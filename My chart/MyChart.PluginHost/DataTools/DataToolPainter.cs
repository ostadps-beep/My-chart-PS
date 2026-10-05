using MyChart.Core.Analysis;
using MyChart.Core.Contracts.Plugins;
using MyChart.Core.Models.Drawing;
using MyChart.Core.Models.Geometry;
using MyChart.Core.Models.Rendering;

namespace MyChart.PluginHost.DataTools;

/// <summary>
/// PG2.04 DataToolPainter — paints Line/Rect from anchors in device pixels.
/// Preview opacity = 60% of object opacity. Line width = max(1 device px, width * DpiScale).
/// </summary>
public sealed class DataToolPainter : IDrawingObjectPainter
{
    private readonly string _shape;

    public DataToolPainter(string shape = "Line") => _shape = shape;

    public void Paint(DrawContext ctx, DrawingObject obj)
    {
        if (obj.Anchors.Count < 1) return;

        double dpi = ctx.DpiScale <= 0 ? 1 : ctx.DpiScale;
        double opacity = obj.Style.Opacity;
        if (ctx.State == DrawState.Preview)
            opacity *= 0.6;

        var color = ApplyOpacity(obj.Style.Color, opacity);
        double widthDip = obj.Style.Thickness;
        double widthPx = Math.Max(1.0, widthDip * dpi);
        double[]? dash = DashScaled(obj.Style.LineStyle, dpi);

        if (obj.Anchors.Count >= 2)
        {
            var a = Screen(obj.Anchors[0], ctx.Map);
            var b = Screen(obj.Anchors[1], ctx.Map);
            // convert DIP screen coords to device pixels
            a = new PointD(a.X * dpi, a.Y * dpi);
            b = new PointD(b.X * dpi, b.Y * dpi);

            if (_shape == "Rect")
            {
                double left = Math.Min(a.X, b.X);
                double top = Math.Min(a.Y, b.Y);
                double w = Math.Abs(b.X - a.X);
                double h = Math.Abs(b.Y - a.Y);
                ctx.Render.StrokeRect((int)left, (int)top, (int)Math.Max(1, w), (int)Math.Max(1, h),
                    (int)Math.Max(1, widthPx), color);
            }
            else
            {
                ctx.Render.DrawLine(a.X, a.Y, b.X, b.Y, color, widthPx, dash);
            }
        }
    }

    private static PointD Screen(DrawingAnchor anchor, IChartMapper map)
    {
        double u = map.IndexOfTime(anchor.TimeUtc);
        return new PointD(map.X(u), map.Y(anchor.Price));
    }

    private static RgbaColor ApplyOpacity(RgbaColor c, double opacity)
    {
        opacity = Math.Clamp(opacity, 0, 1);
        byte a = (byte)Math.Round(((c.Argb >> 24) & 0xFF) * opacity);
        return new RgbaColor((c.Argb & 0x00FFFFFFu) | ((uint)a << 24));
    }

    private static double[]? DashScaled(DrawingLineStyle style, double dpi) => style switch
    {
        DrawingLineStyle.Dashed => new[] { 6.0 * dpi, 4.0 * dpi },
        DrawingLineStyle.Dotted => new[] { 2.0 * dpi, 3.0 * dpi },
        _ => null
    };
}
