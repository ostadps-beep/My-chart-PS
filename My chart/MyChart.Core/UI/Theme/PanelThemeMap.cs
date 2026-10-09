using MyChart.Core.Models.Rendering;

namespace MyChart.Core.UI.Theme;

/// <summary>
/// T7.02 PANEL_THEME — maps Settings Themes/Dark.xaml x:Key names to MyChart theme tokens.
/// Owner visual approval surface = Settings panel body (BgBrush / PanelBrush) after mapping.
/// Original ChartMy panel chrome (reference before map): Bg #1E1E1E, Panel #252525, Accent #4CAF50.
/// </summary>
public static class PanelThemeMap
{
    /// <summary>Original Settings panel body colours (pre-map reference).</summary>
    public static class SettingsPanelReference
    {
        public const string BgColor = "#1E1E1E";
        public const string PanelColor = "#252525";
        public const string ControlColor = "#2A2A2A";
        public const string HoverColor = "#3A3A3A";
        public const string ActiveColor = "#505050";
        public const string BorderColor = "#333333";
        public const string ButtonBorderColor = "#444444";
        public const string TextColor = "#FFFFFF";
        public const string MutedColor = "#AAAAAA";
        public const string AccentColor = "#4CAF50";
    }

    public readonly record struct BrushBinding(string ColorKey, string BrushKey, Func<ThemeTokens, RgbaColor> Resolve);

    /// <summary>All Dark.xaml colour keys mapped to theme tokens.</summary>
    public static IReadOnlyList<BrushBinding> Bindings { get; } = new BrushBinding[]
    {
        new("BgColor", "BgBrush", t => t.BackgroundColor),
        new("PanelColor", "PanelBrush", t => t.GridColor),
        new("ControlColor", "ControlBrush", t => t.GridMajorColor),
        new("HoverColor", "HoverBrush", t => Lighten(t.GridMajorColor, 24)),
        new("ActiveColor", "ActiveBrush", t => Lighten(t.GridMajorColor, 48)),
        new("BorderColor", "BorderBrush", t => t.BorderColor.A == 0 ? t.GridMajorColor : t.BorderColor),
        new("ButtonBorderColor", "ButtonBorderBrush", t => Lighten(t.GridMajorColor, 32)),
        new("TextColor", "TextBrush", t => t.HudColor),
        new("MutedColor", "MutedBrush", t => t.AxisColor),
        new("AccentColor", "AccentBrush", t => t.AccentColor),
    };

    public static IReadOnlyDictionary<string, RgbaColor> ResolveAll(ThemeTokens theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        var map = new Dictionary<string, RgbaColor>(StringComparer.Ordinal);
        foreach (var b in Bindings)
            map[b.ColorKey] = b.Resolve(theme);
        return map;
    }

    /// <summary>
    /// Hex for Settings Dark.xaml static resources = Dark profile after PANEL_THEME map.
    /// Panel body approval colour = BackgroundColor (#131722).
    /// </summary>
    public static IReadOnlyDictionary<string, string> DarkXamlHexFromTheme(ThemeTokens theme)
    {
        var resolved = ResolveAll(theme);
        var hex = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var kv in resolved)
            hex[kv.Key] = ToHex(kv.Value);
        return hex;
    }

    public static string ToHex(RgbaColor c)
        => c.A == 255
            ? $"#{c.R:X2}{c.G:X2}{c.B:X2}"
            : $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";

    private static RgbaColor Lighten(RgbaColor c, int delta)
    {
        byte r = (byte)Math.Clamp(c.R + delta, 0, 255);
        byte g = (byte)Math.Clamp(c.G + delta, 0, 255);
        byte b = (byte)Math.Clamp(c.B + delta, 0, 255);
        return RgbaColor.FromArgb(c.A, r, g, b);
    }
}
