using System.Windows.Media;
using MyChart.Core.Models.Rendering;
using MyChart.Core.UI.Icons;

namespace MyChart.UI.Icons;

/// <summary>T7.01 — builds WPF brushes from <see cref="IconColorRules"/>.</summary>
public static class IconBrushFactory
{
    public static SolidColorBrush Create(ThemeTokens theme, IconVisualState state)
    {
        var c = IconColorRules.Resolve(theme, state);
        var brush = new SolidColorBrush(Color.FromArgb(c.A, c.R, c.G, c.B));
        brush.Freeze();
        return brush;
    }
}
