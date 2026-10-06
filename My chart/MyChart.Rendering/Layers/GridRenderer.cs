using MyChart.Core.Contracts.Rendering;
using MyChart.Core.Contracts.UI;
using MyChart.Core.Models.Viewport;
using MyChart.Core.Scale;

namespace MyChart.Rendering.Layers;

/// <summary>
/// T4.03 Grid — horizontal lines at major price ticks (GridMajorColor);
/// vertical lines at accepted time labels (GridMajorColor).
/// Minor lines optional (default OFF).
/// </summary>
public sealed class GridRenderer
{
    private readonly IThemeService _theme;

    public GridRenderer(IThemeService theme) => _theme = theme;

    public bool DrawMinorLines { get; set; }

    public void Render(
        IRenderContext ctx,
        ViewState view,
        CoordinateConverter converter,
        IReadOnlyList<double> priceTicksTransform,
        IReadOnlyList<double> timeLabelUs,
        double dpiScale)
    {
        double dpi = dpiScale <= 0 ? 1 : dpiScale;
        var color = _theme.Current.GridMajorColor;
        double plotLeft = view.PlotLeft * dpi;
        double plotTop = view.PlotTop * dpi;
        double plotRight = view.PlotRight * dpi;
        double plotBottom = (view.PlotTop + view.PlotHeight) * dpi;

        // Horizontal major price lines
        foreach (var t in priceTicksTransform)
        {
            // t is in transform space; Y expects real price via inverse then Y
            // CoordinateConverter.Y takes real price — callers pass transform-space ticks
            // For Linear, transform == price. Use InverseTransform via ViewState.PriceScale.
            double price = view.PriceScale.InverseTransform(t);
            double y = converter.Y(price) * dpi;
            if (y < plotTop - 0.5 || y > plotBottom + 0.5) continue;
            ctx.DrawLine(plotLeft, y, plotRight, y, color, 1.0, null);
        }

        // Vertical time lines
        foreach (var u in timeLabelUs)
        {
            double x = converter.X(u) * dpi;
            if (x < plotLeft - 0.5 || x > plotRight + 0.5) continue;
            ctx.DrawLine(x, plotTop, x, plotBottom, color, 1.0, null);
        }

        if (DrawMinorLines)
        {
            var minorColor = _theme.Current.GridMinorColor;
            // optional minor — left for callers to supply minor ticks later
            _ = minorColor;
        }
    }
}
