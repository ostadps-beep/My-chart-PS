namespace MyChart.Core.Models.Rendering;

/// <summary>
/// T4.02 / T7.02 — single color system.
/// Owner (2026-10-09): global chrome = ChartMy Settings Themes/Dark.xaml (one palette, no dual theme).
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

    /// <summary>Panel chrome extras (Settings / toolbars).</summary>
    public RgbaColor ControlColor { get; init; }
    public RgbaColor HoverColor { get; init; }
    public RgbaColor ActiveColor { get; init; }
    public RgbaColor ButtonBorderColor { get; init; }

    public required IReadOnlyList<RgbaColor> IndicatorPalette { get; init; }

    public RgbaColor IndicatorColor(int index)
    {
        if (IndicatorPalette.Count == 0) return AccentColor;
        return IndicatorPalette[Math.Abs(index) % IndicatorPalette.Count];
    }

    /// <summary>
    /// Official Dark = ChartMy panel palette (extracted Themes/Dark.xaml).
    /// Bg #1E1E1E, Panel #252525, Control #2A2A2A, Hover #3A3A3A, Active #505050,
    /// Border #333333, ButtonBorder #444444, Text #FFFFFF, Muted #AAAAAA, Accent #4CAF50.
    /// </summary>
    public static ThemeTokens Dark { get; } = CreateDark();

    public static ThemeTokens Light { get; } = CreateLight();

    public static ThemeTokens ProDark { get; } = CreateProDark();

    public static ThemeTokens ForProfile(ThemeProfileKind kind) => kind switch
    {
        ThemeProfileKind.Light => Light,
        ThemeProfileKind.ProDark => ProDark,
        ThemeProfileKind.Custom => Dark, // base until overrides applied
        _ => Dark
    };

    /// <summary>Custom = base profile + per-token hex overrides (#RRGGBB or #AARRGGBB).</summary>
    public static ThemeTokens ApplyOverrides(ThemeTokens baseTokens, IReadOnlyDictionary<string, string> overrides)
    {
        if (overrides is null || overrides.Count == 0)
            return baseTokens;

        RgbaColor T(string key, RgbaColor current)
        {
            if (overrides.TryGetValue(key, out var hex) && !string.IsNullOrWhiteSpace(hex))
            {
                try { return RgbaColor.ParseHex(hex); }
                catch { /* ignore bad hex */ }
            }
            return current;
        }

        return new ThemeTokens
        {
            BackgroundColor = T("Background", baseTokens.BackgroundColor),
            GridColor = T("Grid", baseTokens.GridColor),
            GridMajorColor = T("GridMajor", baseTokens.GridMajorColor),
            GridMinorColor = T("GridMinor", baseTokens.GridMinorColor),
            AxisColor = T("Axis", baseTokens.AxisColor),
            HudColor = T("Hud", baseTokens.HudColor),
            AccentColor = T("Accent", baseTokens.AccentColor),
            SelectionColor = T("Selection", baseTokens.SelectionColor),
            BullColor = T("Bull", baseTokens.BullColor),
            BearColor = T("Bear", baseTokens.BearColor),
            WarningColor = T("Warning", baseTokens.WarningColor),
            ErrorColor = T("Error", baseTokens.ErrorColor),
            SuccessColor = T("Success", baseTokens.SuccessColor),
            WickColor = T("Wick", baseTokens.WickColor),
            BorderColor = T("Border", baseTokens.BorderColor),
            ControlColor = T("Control", baseTokens.ControlColor),
            HoverColor = T("Hover", baseTokens.HoverColor),
            ActiveColor = T("Active", baseTokens.ActiveColor),
            ButtonBorderColor = T("ButtonBorder", baseTokens.ButtonBorderColor),
            IndicatorPalette = baseTokens.IndicatorPalette
        };
    }

    private static ThemeTokens CreateDark()
    {
        var accent = RgbaColor.ParseHex("#4CAF50");
        return new ThemeTokens
        {
            BackgroundColor = RgbaColor.ParseHex("#1E1E1E"),      // BgColor
            GridColor = RgbaColor.ParseHex("#252525"),            // PanelColor
            GridMajorColor = RgbaColor.ParseHex("#2A2A2A"),       // ControlColor
            GridMinorColor = RgbaColor.ParseHex("#252525"),       // PanelColor
            AxisColor = RgbaColor.ParseHex("#AAAAAA"),            // MutedColor
            HudColor = RgbaColor.ParseHex("#FFFFFF"),             // TextColor
            AccentColor = accent,                                   // AccentColor
            SelectionColor = accent,
            ControlColor = RgbaColor.ParseHex("#2A2A2A"),
            HoverColor = RgbaColor.ParseHex("#3A3A3A"),
            ActiveColor = RgbaColor.ParseHex("#505050"),
            BorderColor = RgbaColor.ParseHex("#333333"),
            ButtonBorderColor = RgbaColor.ParseHex("#444444"),
            // Semantic (candles / status) — from ChartMy schema defaults
            BullColor = RgbaColor.ParseHex("#26A69A"),
            BearColor = RgbaColor.ParseHex("#EF5350"),
            WarningColor = RgbaColor.ParseHex("#FF9800"),
            ErrorColor = RgbaColor.ParseHex("#EF5350"),
            SuccessColor = accent,
            WickColor = RgbaColor.ParseHex("#CCCCCC"),
            IndicatorPalette = new[]
            {
                accent,
                RgbaColor.ParseHex("#FF9800"),
                RgbaColor.ParseHex("#E040FB"),
                RgbaColor.ParseHex("#00BCD4"),
                RgbaColor.ParseHex("#FFEB3B"),
                RgbaColor.ParseHex("#8BC34A"),
            }
        };
    }

    private static ThemeTokens CreateLight()
    {
        // Light chrome derived from same family (inverted surfaces); accent stays ChartMy green.
        var accent = RgbaColor.ParseHex("#4CAF50");
        return new ThemeTokens
        {
            BackgroundColor = RgbaColor.ParseHex("#FFFFFF"),
            GridColor = RgbaColor.ParseHex("#F0F0F0"),
            GridMajorColor = RgbaColor.ParseHex("#E0E0E0"),
            GridMinorColor = RgbaColor.ParseHex("#F0F0F0"),
            AxisColor = RgbaColor.ParseHex("#666666"),
            HudColor = RgbaColor.ParseHex("#1E1E1E"),
            AccentColor = accent,
            SelectionColor = accent,
            ControlColor = RgbaColor.ParseHex("#F5F5F5"),
            HoverColor = RgbaColor.ParseHex("#EEEEEE"),
            ActiveColor = RgbaColor.ParseHex("#E0E0E0"),
            BorderColor = RgbaColor.ParseHex("#CCCCCC"),
            ButtonBorderColor = RgbaColor.ParseHex("#BDBDBD"),
            BullColor = RgbaColor.ParseHex("#089981"),
            BearColor = RgbaColor.ParseHex("#F23645"),
            WarningColor = RgbaColor.ParseHex("#FF9800"),
            ErrorColor = RgbaColor.ParseHex("#F23645"),
            SuccessColor = accent,
            WickColor = RgbaColor.ParseHex("#666666"),
            IndicatorPalette = Dark.IndicatorPalette
        };
    }

    private static ThemeTokens CreateProDark()
    {
        // Deeper surfaces; same accent family as ChartMy panel.
        var accent = RgbaColor.ParseHex("#4CAF50");
        return new ThemeTokens
        {
            BackgroundColor = RgbaColor.ParseHex("#121212"),
            GridColor = RgbaColor.ParseHex("#1A1A1A"),
            GridMajorColor = RgbaColor.ParseHex("#242424"),
            GridMinorColor = RgbaColor.ParseHex("#1A1A1A"),
            AxisColor = RgbaColor.ParseHex("#9E9E9E"),
            HudColor = RgbaColor.ParseHex("#EEEEEE"),
            AccentColor = accent,
            SelectionColor = accent,
            ControlColor = RgbaColor.ParseHex("#242424"),
            HoverColor = RgbaColor.ParseHex("#333333"),
            ActiveColor = RgbaColor.ParseHex("#424242"),
            BorderColor = RgbaColor.ParseHex("#2C2C2C"),
            ButtonBorderColor = RgbaColor.ParseHex("#3D3D3D"),
            BullColor = RgbaColor.ParseHex("#26A69A"),
            BearColor = RgbaColor.ParseHex("#EF5350"),
            WarningColor = RgbaColor.ParseHex("#FF9800"),
            ErrorColor = RgbaColor.ParseHex("#EF5350"),
            SuccessColor = accent,
            WickColor = RgbaColor.ParseHex("#BDBDBD"),
            IndicatorPalette = Dark.IndicatorPalette
        };
    }
}
