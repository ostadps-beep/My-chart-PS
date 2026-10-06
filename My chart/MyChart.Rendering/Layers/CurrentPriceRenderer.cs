using System.Globalization;
using MyChart.Core.Contracts.Rendering;
using MyChart.Core.Contracts.UI;
using MyChart.Core.Models.Rendering;
using MyChart.Core.Models.Viewport;
using MyChart.Core.Scale;

namespace MyChart.Rendering.Layers;

/// <summary>
/// T4.05 Current price line + market axis marker.
/// Line: dashed [4,4] 1 px, 40% opacity, bull/bear color.
/// AxisMarker: filled rect on price axis with price and optional countdown.
/// </summary>
public sealed class CurrentPriceRenderer
{
    private readonly IThemeService _theme;

    public CurrentPriceRenderer(IThemeService theme) => _theme = theme;

    public void Render(
        IRenderContext ctx,
        ViewState view,
        CoordinateConverter converter,
        double price,
        bool isBull,
        string? countdownText,
        int digits,
        double dpiScale)
    {
        double dpi = dpiScale <= 0 ? 1 : dpiScale;
        var tokens = _theme.Current;

        double priceY = converter.Y(price);
        var (clampedY, _) = CurrentPriceValues.ClampMarkerY(priceY, view);
        double y = clampedY * dpi;

        double plotLeft = view.PlotLeft * dpi;
        double plotRight = view.PlotRight * dpi;

        RgbaColor baseColor = isBull ? tokens.BullColor : tokens.BearColor;
        RgbaColor lineColor = WithOpacity(baseColor, 0.40);

        // Horizontal dashed line across plot
        ctx.DrawLine(plotLeft, y, plotRight, y, lineColor, 1.0, new[] { 4.0 * dpi, 4.0 * dpi });

        // Axis marker (market) — filled strip on price axis
        PaintAxisMarker(ctx, view, y, price, countdownText, digits, baseColor, dpi);
    }

    /// <summary>Analysis (crosshair) marker drawn above the market marker when both visible.</summary>
    public void RenderAnalysisMarker(
        IRenderContext ctx,
        ViewState view,
        double priceYDip,
        double price,
        int digits,
        double dpiScale)
    {
        double dpi = dpiScale <= 0 ? 1 : dpiScale;
        var tokens = _theme.Current;
        var (clampedY, _) = CurrentPriceValues.ClampMarkerY(priceYDip, view);
        double y = clampedY * dpi;

        // Slightly different fill: Accent at 80%
        var fill = WithOpacity(tokens.AccentColor, 0.85);
        PaintAxisMarker(ctx, view, y, price, countdownText: null, digits, fill, dpi, offsetDip: -14);
    }

    private static void PaintAxisMarker(
        IRenderContext ctx,
        ViewState view,
        double yPx,
        double price,
        string? countdownText,
        int digits,
        RgbaColor fill,
        double dpi,
        double offsetDip = 0)
    {
        string priceText = price.ToString("F" + digits, CultureInfo.InvariantCulture);
        string label = string.IsNullOrEmpty(countdownText) ? priceText : priceText + "  " + countdownText;

        var style = new TextStyle("Consolas", 11 * dpi, Bold: false);
        var size = ctx.MeasureText(label, style);

        double axisLeft = view.PlotRight * dpi;
        double padX = 4 * dpi;
        double padY = 2 * dpi;
        double boxH = size.Height + padY * 2;
        double boxW = Math.Max(view.PriceAxisWidth * dpi - 4 * dpi, size.Width + padX * 2);
        double boxX = axisLeft + 2 * dpi;
        double boxY = yPx - boxH / 2 + offsetDip * dpi;

        ctx.FillRect((int)boxX, (int)boxY, (int)Math.Ceiling(boxW), (int)Math.Ceiling(boxH), fill);

        // Contrasting text: white on colored fill
        var textColor = RgbaColor.FromRgb(255, 255, 255);
        ctx.DrawText(label, boxX + padX, boxY + padY + size.Height * 0.8, style, textColor, TextAlign.Left);
    }

    private static RgbaColor WithOpacity(RgbaColor c, double opacity)
    {
        opacity = Math.Clamp(opacity, 0, 1);
        byte a = (byte)Math.Round(0xFF * opacity);
        return new RgbaColor((c.Argb & 0x00FFFFFFu) | ((uint)a << 24));
    }
}
