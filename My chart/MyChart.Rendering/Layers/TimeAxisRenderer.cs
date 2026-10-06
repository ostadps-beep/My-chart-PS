using MyChart.Core.Contracts.Rendering;
using MyChart.Core.Contracts.UI;
using MyChart.Core.Models.Rendering;
using MyChart.Core.Models.Viewport;
using MyChart.Core.Scale;

namespace MyChart.Rendering.Layers;

/// <summary>
/// T4.03 TimeAxis — 24 DIP high at the bottom. Labels in AxisColor.
/// Axis line 1 device px in GridMajorColor.
/// </summary>
public sealed class TimeAxisRenderer
{
    private readonly IThemeService _theme;
    public const double AxisHeightDip = 24;
    public const double LabelSizeDip = 11;

    public TimeAxisRenderer(IThemeService theme) => _theme = theme;

    public void Render(
        IRenderContext ctx,
        ViewState view,
        CoordinateConverter converter,
        IReadOnlyList<(double U, string Label)> labels,
        double dpiScale)
    {
        double dpi = dpiScale <= 0 ? 1 : dpiScale;
        var tokens = _theme.Current;
        double plotLeft = view.PlotLeft * dpi;
        double plotRight = view.PlotRight * dpi;
        double plotBottom = (view.PlotTop + view.PlotHeight) * dpi;

        ctx.DrawLine(plotLeft, plotBottom, plotRight, plotBottom, tokens.GridMajorColor, 1.0, null);

        var style = new TextStyle("Consolas", LabelSizeDip * dpi, Bold: false);
        double textY = plotBottom + 4 * dpi;
        foreach (var (u, label) in labels)
        {
            double x = converter.X(u) * dpi;
            if (x < plotLeft - 0.5 || x > plotRight + 0.5) continue;
            ctx.DrawText(label, x, textY, style, tokens.AxisColor, TextAlign.Center);
        }
    }
}
