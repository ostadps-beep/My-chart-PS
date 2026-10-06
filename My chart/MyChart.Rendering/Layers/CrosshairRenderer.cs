using MyChart.Core.Contracts.Rendering;
using MyChart.Core.Contracts.UI;
using MyChart.Core.Models.Chart;
using MyChart.Core.Models.Rendering;
using MyChart.Core.Models.Viewport;

namespace MyChart.Rendering.Layers;

/// <summary>
/// T4.06 CrosshairRenderer — visible only inside the plot.
/// Lines 1 px dashed [4,4] AxisColor @ 60% opacity.
/// Vertical at X(iSnap); horizontal at cursor Y (or magnet price).
/// Labels on both axes; Analysis box near cursor when present.
/// </summary>
public sealed class CrosshairRenderer
{
    private readonly IThemeService _theme;

    public CrosshairRenderer(IThemeService theme) => _theme = theme;

    public void Render(
        IRenderContext ctx,
        ViewState view,
        CrosshairState state,
        double dpiScale)
    {
        if (!state.IsInsidePlot)
            return;

        double dpi = dpiScale <= 0 ? 1 : dpiScale;
        var tokens = _theme.Current;
        var lineColor = WithOpacity(tokens.AxisColor, 0.60);
        double[] dash = { 4.0 * dpi, 4.0 * dpi };

        double plotLeft = view.PlotLeft * dpi;
        double plotRight = view.PlotRight * dpi;
        double plotTop = view.PlotTop * dpi;
        double plotBottom = (view.PlotTop + view.PlotHeight) * dpi;

        double x = state.X * dpi;
        double y = state.Y * dpi;

        // Vertical + horizontal dashed lines
        ctx.DrawLine(x, plotTop, x, plotBottom, lineColor, 1.0, dash);
        ctx.DrawLine(plotLeft, y, plotRight, y, lineColor, 1.0, dash);

        // Price label on price axis
        PaintAxisLabel(ctx, view, state.PriceLabel, isPrice: true, x, y, dpi, tokens);

        // Time label on time axis
        PaintAxisLabel(ctx, view, state.TimeLabel, isPrice: false, x, y, dpi, tokens);

        // Analysis box near cursor
        if (state.Analysis is { } analysis)
            PaintAnalysisBox(ctx, analysis, x, y, dpi, tokens);
    }

    private static void PaintAxisLabel(
        IRenderContext ctx,
        ViewState view,
        string text,
        bool isPrice,
        double x,
        double y,
        double dpi,
        ThemeTokens tokens)
    {
        if (string.IsNullOrEmpty(text)) return;

        var style = new TextStyle("Consolas", 11 * dpi, Bold: false);
        var size = ctx.MeasureText(text, style);
        double pad = 3 * dpi;
        var bg = WithOpacity(tokens.BackgroundColor, 0.85);
        var fg = tokens.HudColor;

        if (isPrice)
        {
            double boxX = view.PlotRight * dpi + 2 * dpi;
            double boxY = y - size.Height / 2 - pad;
            double boxW = Math.Max(view.PriceAxisWidth * dpi - 4 * dpi, size.Width + pad * 2);
            double boxH = size.Height + pad * 2;
            ctx.FillRect((int)boxX, (int)boxY, (int)Math.Ceiling(boxW), (int)Math.Ceiling(boxH), bg);
            ctx.DrawText(text, boxX + pad, boxY + pad + size.Height * 0.8, style, fg, TextAlign.Left);
        }
        else
        {
            double boxW = size.Width + pad * 2;
            double boxH = size.Height + pad * 2;
            double boxX = x - boxW / 2;
            double boxY = (view.PlotTop + view.PlotHeight) * dpi + 2 * dpi;
            ctx.FillRect((int)boxX, (int)boxY, (int)Math.Ceiling(boxW), (int)Math.Ceiling(boxH), bg);
            ctx.DrawText(text, x, boxY + pad + size.Height * 0.8, style, fg, TextAlign.Center);
        }
    }

    private static void PaintAnalysisBox(
        IRenderContext ctx,
        AnalysisValues a,
        double x,
        double y,
        double dpi,
        ThemeTokens tokens)
    {
        var lines = new List<string>
        {
            $"ΔP {a.PriceDifference:F5}",
            $"Pts {a.PointDifference}",
            $"Bars {a.CandleCount}",
            $"% {a.PercentageChange:F2}"
        };
        if (a.PipDifference is { } pip)
            lines.Insert(2, $"Pips {pip:F1}");

        var style = new TextStyle("Consolas", 11 * dpi, Bold: false);
        double lineH = 14 * dpi;
        double pad = 6 * dpi;
        double maxW = 0;
        foreach (var line in lines)
            maxW = Math.Max(maxW, ctx.MeasureText(line, style).Width);

        double boxW = maxW + pad * 2;
        double boxH = lines.Count * lineH + pad * 2;
        double boxX = x + 12 * dpi;
        double boxY = y - boxH - 8 * dpi;

        var bg = WithOpacity(tokens.BackgroundColor, 0.90);
        ctx.FillRect((int)boxX, (int)boxY, (int)Math.Ceiling(boxW), (int)Math.Ceiling(boxH), bg);

        for (int i = 0; i < lines.Count; i++)
        {
            ctx.DrawText(
                lines[i],
                boxX + pad,
                boxY + pad + (i + 0.8) * lineH,
                style,
                tokens.HudColor,
                TextAlign.Left);
        }
    }

    private static RgbaColor WithOpacity(RgbaColor c, double opacity)
    {
        opacity = Math.Clamp(opacity, 0, 1);
        byte a = (byte)Math.Round(0xFF * opacity);
        return new RgbaColor((c.Argb & 0x00FFFFFFu) | ((uint)a << 24));
    }
}
