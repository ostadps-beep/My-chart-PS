using MyChart.Core.Contracts.Rendering;
using MyChart.Core.Contracts.UI;
using MyChart.Core.Models.Rendering;
using MyChart.Core.Models.Viewport;

namespace MyChart.Rendering.Layers;

/// <summary>T4.03 Background — fill BackgroundColor over the full surface.</summary>
public sealed class BackgroundRenderer
{
    private readonly IThemeService _theme;

    public BackgroundRenderer(IThemeService theme) => _theme = theme;

    public void Render(IRenderContext ctx, ViewState view, double dpiScale)
    {
        double dpi = dpiScale <= 0 ? 1 : dpiScale;
        int w = (int)Math.Ceiling(view.Width * dpi);
        int h = (int)Math.Ceiling(view.Height * dpi);
        if (w <= 0 || h <= 0)
        {
            w = ctx.Width;
            h = ctx.Height;
        }
        ctx.FillRect(0, 0, w, h, _theme.Current.BackgroundColor);
    }
}
