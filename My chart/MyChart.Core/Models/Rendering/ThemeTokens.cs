namespace MyChart.Core.Models.Rendering;

/// <summary>
/// T4.02 / C1 — single color system. Renderers via IThemeService only.
/// Dark defaults match CORRECTIONS_2026-10-10 (Settings panel source of truth).
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

    public required IReadOnlyList<RgbaColor> IndicatorPalette { get; init; }

    public RgbaColor IndicatorColor(int index)
    {
        if (IndicatorPalette.Count == 0) return AccentColor;
        return IndicatorPalette[Math.Abs(index) % IndicatorPalette.Count];
    }

    /// <summary>Same as ThemeService.Dark (C1). Kept for callers that use ThemeTokens.Dark.</summary>
    public static ThemeTokens Dark { get; } = new()
    {
        BackgroundColor = RgbaColor.ParseHex("#1E1E1E"),
        BullColor = RgbaColor.ParseHex("#26A69A"),
        BearColor = RgbaColor.ParseHex("#EF5350"),
        GridColor = RgbaColor.ParseHex("#2A2E39"),
        GridMajorColor = RgbaColor.ParseHex("#2A2E39"),
        GridMinorColor = RgbaColor.FromArgb(128, 0x2A, 0x2E, 0x39),
        AxisColor = RgbaColor.ParseHex("#888888"),
        HudColor = RgbaColor.ParseHex("#FFFFFF"),
        AccentColor = RgbaColor.ParseHex("#2962FF"),
        SelectionColor = RgbaColor.ParseHex("#2962FF"),
        WarningColor = RgbaColor.ParseHex("#FF9800"),
        ErrorColor = RgbaColor.ParseHex("#F23645"),
        SuccessColor = RgbaColor.ParseHex("#089981"),
        WickColor = RgbaColor.ParseHex("#CCCCCC"),
        BorderColor = RgbaColor.ParseHex("#1A1A1A"),
        IndicatorPalette = new[]
        {
            RgbaColor.ParseHex("#2962FF"),
            RgbaColor.ParseHex("#FF9800"),
            RgbaColor.ParseHex("#E040FB"),
            RgbaColor.ParseHex("#00BCD4"),
            RgbaColor.ParseHex("#FFEB3B"),
            RgbaColor.ParseHex("#8BC34A"),
        }
    };
}
