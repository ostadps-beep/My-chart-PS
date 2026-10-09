using MyChart.Core.Models.Rendering;

namespace MyChart.Core.UI.Icons;

/// <summary>
/// T7.01 colour rules: normal = AxisColor; hover/active = AccentColor;
/// disabled = AxisColor at 40 percent opacity.
/// </summary>
public static class IconColorRules
{
    /// <summary>Disabled icon opacity factor (spec: 40 percent).</summary>
    public const double DisabledOpacity = 0.40;

    public static RgbaColor Resolve(ThemeTokens theme, IconVisualState state)
    {
        ArgumentNullException.ThrowIfNull(theme);
        return state switch
        {
            IconVisualState.Hover or IconVisualState.Active => theme.AccentColor,
            IconVisualState.Disabled => WithOpacity(theme.AxisColor, DisabledOpacity),
            _ => theme.AxisColor
        };
    }

    private static RgbaColor WithOpacity(RgbaColor c, double opacity)
    {
        byte a = (byte)Math.Clamp(Math.Round(c.A * opacity), 0, 255);
        return RgbaColor.FromArgb(a, c.R, c.G, c.B);
    }
}
