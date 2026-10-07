using MyChart.Core.Contracts.Rendering;
using MyChart.Core.Contracts.UI;
using MyChart.Core.Models.Geometry;
using MyChart.Core.Models.Indicators;
using MyChart.Core.Models.Rendering;
using MyChart.Core.Scale;

namespace MyChart.Rendering.Layers;

/// <summary>
/// T6.06 Indicator layer (RenderLayer 4) — polyline through visible outputs.
/// NaN breaks the line. Colors from IndicatorPalette in order (theme).
/// </summary>
public sealed class IndicatorRenderer
{
    private readonly IThemeService _theme;

    public IndicatorRenderer(IThemeService theme) => _theme = theme;

    public double LineWidthDip { get; set; } = 1.5;

    /// <summary>
    /// Draw one or more indicator outputs for bars [firstIndex .. firstIndex+values.Length).
    /// paletteOffset selects the starting color in IndicatorPalette.
    /// </summary>
    public void Render(
        IRenderContext ctx,
        CoordinateConverter converter,
        IReadOnlyList<IndicatorOutput> outputs,
        int firstIndex,
        double dpiScale,
        int paletteOffset = 0)
    {
        if (outputs is null || outputs.Count == 0) return;
        double dpi = dpiScale <= 0 ? 1 : dpiScale;
        var palette = _theme.Current.IndicatorPalette;
        if (palette is null || palette.Length == 0) return;

        double width = LineWidthDip * dpi;

        for (int o = 0; o < outputs.Count; o++)
        {
            var series = outputs[o].Values;
            if (series.Length == 0) continue;
            var color = palette[(paletteOffset + o) % palette.Length];
            DrawBrokenPolyline(ctx, converter, series, firstIndex, color, width, dpi);
        }
    }

    /// <summary>Split on NaN: each contiguous finite segment is one DrawPath (open stroke).</summary>
    public static void DrawBrokenPolyline(
        IRenderContext ctx,
        CoordinateConverter converter,
        double[] values,
        int firstIndex,
        RgbaColor color,
        double widthPx,
        double dpi)
    {
        var segment = new List<PointD>();
        for (int i = 0; i < values.Length; i++)
        {
            double v = values[i];
            if (double.IsNaN(v) || double.IsInfinity(v))
            {
                Flush(ctx, segment, color, widthPx);
                segment.Clear();
                continue;
            }

            int barIndex = firstIndex + i;
            double x = converter.X(barIndex) * dpi;
            double y = converter.Y(v) * dpi;
            segment.Add(new PointD(x, y));
        }
        Flush(ctx, segment, color, widthPx);
    }

    private static void Flush(IRenderContext ctx, List<PointD> segment, RgbaColor color, double widthPx)
    {
        if (segment.Count < 2) return;
        ctx.DrawPath(segment.ToArray(), color, widthPx, closed: false, fill: false);
    }
}
