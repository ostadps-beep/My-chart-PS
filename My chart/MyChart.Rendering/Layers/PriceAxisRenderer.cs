using System.Globalization;
using MyChart.Core.Contracts.Rendering;
using MyChart.Core.Contracts.UI;
using MyChart.Core.Models.Rendering;
using MyChart.Core.Models.Viewport;
using MyChart.Core.Scale;

namespace MyChart.Rendering.Layers;

/// <summary>
/// T4.03 PriceAxis — fixed 64 DIP wide, right side default, labels right-aligned,
/// monospaced 11 px, AxisColor. Axis line 1 device px in GridMajorColor.
/// </summary>
public sealed class PriceAxisRenderer
{
    private readonly IThemeService _theme;
    public const double AxisWidthDip = 64;
    public const double LabelSizeDip = 11;

    public PriceAxisRenderer(IThemeService theme) => _theme = theme;

    public void Render(
        IRenderContext ctx,
        ViewState view,
        CoordinateConverter converter,
        IReadOnlyList<double> priceTicksTransform,
        double step,
        int digits,
        double dpiScale)
    {
        double dpi = dpiScale <= 0 ? 1 : dpiScale;
        var tokens = _theme.Current;
        double plotTop = view.PlotTop * dpi;
        double plotBottom = (view.PlotTop + view.PlotHeight) * dpi;
        double plotRight = view.PlotRight * dpi;

        // Axis line at plot right edge
        ctx.DrawLine(plotRight, plotTop, plotRight, plotBottom, tokens.GridMajorColor, 1.0, null);

        var style = new TextStyle("Consolas", LabelSizeDip * dpi, Bold: false);
        foreach (var t in priceTicksTransform)
        {
            double price = view.PriceScale.InverseTransform(t);
            double y = converter.Y(price) * dpi;
            if (y < plotTop - 0.5 || y > plotBottom + 0.5) continue;

            string label = NiceTicks.FormatPriceLabel(price, step, digits, view.PriceScale.TransformKind);
            // right-aligned inside axis strip
            double textX = (view.Width - 4) * dpi;
            ctx.DrawText(label, textX, y, style, tokens.AxisColor, TextAlign.Right);
        }
    }
}
