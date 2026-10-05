using MyChart.Core.Contracts.Rendering;
using MyChart.Core.Models.Rendering;

namespace MyChart.Rendering.Skia;

/// <summary>
/// Extension methods for IRenderContext (T4.01).
/// Common drawing patterns to reduce duplication.
/// </summary>
public static class SkiaRenderContextExtensions
{
    /// <summary>Draw a dashed horizontal line (common for price markers).</summary>
    public static void DrawDashedHorizontalLine(
        this IRenderContext ctx,
        double y, double xStart, double xEnd,
        RgbaColor color, double width = 1.0)
    {
        ctx.DrawLine(xStart, y, xEnd, y, color, width, new[] { 4.0, 4.0 });
    }

    /// <summary>Draw a filled rectangle with optional border.</summary>
    public static void DrawRectWithBorder(
        this IRenderContext ctx,
        int x, int y, int w, int h,
        RgbaColor fillColor, RgbaColor borderColor, int borderThickness)
    {
        ctx.FillRect(x, y, w, h, fillColor);
        if (borderThickness > 0)
            ctx.StrokeRect(x, y, w, h, borderThickness, borderColor);
    }
}
