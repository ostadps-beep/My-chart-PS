namespace MyChart.Core.Models.Rendering;

/// <summary>
/// T4.02 ColorTokensMinimal — single color system.
/// Renderers read colors only through IThemeService; no hex literal in renderers.
/// </summary>
public sealed class ThemeTokens
{
    public required RgbaColor BullColor { get; init; }
    public required RgbaColor BearColor { get; init; }
    public required RgbaColor GridColor { get; init; }
    public required RgbaColor GridMajorColor { get; init; }
    public required RgbaColor GridMinorColor { get; init; }
    public required RgbaColor AxisColor { get; init; }
    public required RgbaColor HudColor { get; init; }
    public required RgbaColor AccentColor { get; init; }
    public required RgbaColor BackgroundColor { get; init; }
    public required RgbaColor SelectionColor { get; init; }
    public required RgbaColor WarningColor { get; init; }
    public required RgbaColor ErrorColor { get; init; }
    public required RgbaColor SuccessColor { get; init; }

    public RgbaColor WickColor { get; init; }
    public RgbaColor BorderColor { get; init; }

    /// <summary>IndicatorPalette = [#2962FF, #FF9800, #E040FB, #00BCD4, #FFEB3B, #8BC34A].</summary>
    public required IReadOnlyList<RgbaColor> IndicatorPalette { get; init; }

    public RgbaColor IndicatorColor(int index)
    {
        if (IndicatorPalette.Count == 0) return AccentColor;
        return IndicatorPalette[Math.Abs(index) % IndicatorPalette.Count];
    }
}
