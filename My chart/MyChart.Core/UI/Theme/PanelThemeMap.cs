using MyChart.Core.Models.Rendering;

namespace MyChart.Core.UI.Theme;

/// <summary>
/// T7.02 PANEL_THEME — maps Settings Themes/Dark.xaml x:Key names to ThemeTokens.
/// Owner palette = ChartMy extraction (one global system).
/// </summary>
public static class PanelThemeMap
{
    /// <summary>x:Key in Themes/Dark.xaml → hex from current tokens.</summary>
    public static IReadOnlyDictionary<string, string> ToPanelHex(ThemeTokens t) => new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["BgColor"] = ToHex(t.BackgroundColor),
        ["PanelColor"] = ToHex(t.GridColor),
        ["ControlColor"] = ToHex(t.ControlColor.Argb != 0 ? t.ControlColor : t.GridMajorColor),
        ["HoverColor"] = ToHex(t.HoverColor),
        ["ActiveColor"] = ToHex(t.ActiveColor),
        ["BorderColor"] = ToHex(t.BorderColor),
        ["ButtonBorderColor"] = ToHex(t.ButtonBorderColor),
        ["TextColor"] = ToHex(t.HudColor),
        ["MutedColor"] = ToHex(t.AxisColor),
        ["AccentColor"] = ToHex(t.AccentColor),
    };

    public static bool MatchesChartMyOfficial(ThemeTokens t)
    {
        var map = ToPanelHex(t);
        return map["BgColor"] == "#1E1E1E"
            && map["PanelColor"] == "#252525"
            && map["ControlColor"] == "#2A2A2A"
            && map["HoverColor"] == "#3A3A3A"
            && map["ActiveColor"] == "#505050"
            && map["BorderColor"] == "#333333"
            && map["ButtonBorderColor"] == "#444444"
            && map["TextColor"] == "#FFFFFF"
            && map["MutedColor"] == "#AAAAAA"
            && map["AccentColor"] == "#4CAF50";
    }

    private static string ToHex(RgbaColor c)
    {
        // Prefer #RRGGBB when opaque
        var a = (byte)((c.Argb >> 24) & 0xFF);
        var r = (byte)((c.Argb >> 16) & 0xFF);
        var g = (byte)((c.Argb >> 8) & 0xFF);
        var b = (byte)(c.Argb & 0xFF);
        return a == 255 ? $"#{r:X2}{g:X2}{b:X2}" : $"#{a:X2}{r:X2}{g:X2}{b:X2}";
    }
}
