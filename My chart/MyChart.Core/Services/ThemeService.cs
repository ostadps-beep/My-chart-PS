using MyChart.Core.Contracts.UI;
using MyChart.Core.Models.Rendering;
using MyChart.Core.Models.Settings;

namespace MyChart.Core.Services;

/// <summary>
/// T4.02 / T7.02 / C1 — single Dark palette = Settings panel (CORRECTIONS_2026-10-10).
/// </summary>
public sealed class ThemeService : IThemeService
{
    private static readonly RgbaColor[] IndicatorPaletteColors =
    {
        RgbaColor.ParseHex("#2962FF"),
        RgbaColor.ParseHex("#FF9800"),
        RgbaColor.ParseHex("#E040FB"),
        RgbaColor.ParseHex("#00BCD4"),
        RgbaColor.ParseHex("#FFEB3B"),
        RgbaColor.ParseHex("#8BC34A"),
    };

    /// <summary>C1 Dark: chrome + chart from Settings Dark.xaml / SettingsSchema.</summary>
    public static ThemeTokens Dark { get; } = Build(
        background: "#1E1E1E",
        grid: "#2A2E39",
        gridMajor: "#2A2E39",
        gridMinor: null, // 50% of #2A2E39
        axis: "#888888",
        hud: "#FFFFFF",
        accent: "#2962FF",
        selection: "#2962FF",
        bull: "#26A69A",
        bear: "#EF5350",
        warning: "#FF9800",
        error: "#F23645",
        success: "#089981",
        wick: "#CCCCCC",
        border: "#1A1A1A");

    public static ThemeTokens ProDark { get; } = Build(
        background: "#0B0E14",
        grid: "#161A23",
        gridMajor: "#1F2430",
        gridMinor: "#161A23",
        axis: "#6B6F7B",
        hud: "#A9ACB8",
        accent: "#2962FF",
        selection: "#2962FF",
        bull: "#26A69A",
        bear: "#EF5350",
        warning: "#FF9800",
        error: "#F23645",
        success: "#089981");

    public static ThemeTokens Light { get; } = Build(
        background: "#FFFFFF",
        grid: "#F0F3FA",
        gridMajor: "#E0E3EB",
        gridMinor: "#F0F3FA",
        axis: "#6A6D78",
        hud: "#434651",
        accent: "#2962FF",
        selection: "#2962FF",
        bull: "#089981",
        bear: "#F23645",
        warning: "#FF9800",
        error: "#F23645",
        success: "#089981",
        wick: "#666666",
        border: "#E0E3EB");

    public ThemeService(string profileName = "Dark")
    {
        SetProfile(ParseProfile(profileName), overrides: null);
    }

    public string ProfileName { get; private set; } = "Dark";
    public ThemeProfile Profile { get; private set; } = ThemeProfile.Dark;
    public ThemeProfile BaseProfile { get; private set; } = ThemeProfile.Dark;
    public ThemeTokens Current { get; private set; } = Dark;

    public event Action? Changed;

    public void SetProfile(ThemeProfile profile, ChartSettingValues? overrides = null)
    {
        if (profile == ThemeProfile.Custom)
        {
            if (Profile != ThemeProfile.Custom)
                BaseProfile = Profile;
            var basisName = BaseProfile is ThemeProfile.Light or ThemeProfile.ProDark
                ? BaseProfile
                : ThemeProfile.Dark;
            Current = ApplyOverrides(ProfileTokens(basisName), overrides);
            Profile = ThemeProfile.Custom;
            ProfileName = "Custom";
        }
        else
        {
            BaseProfile = profile;
            Profile = profile;
            ProfileName = profile.ToString();
            Current = overrides is null
                ? ProfileTokens(profile)
                : ApplyOverrides(ProfileTokens(profile), overrides);
        }

        Changed?.Invoke();
    }

    public void ApplyFromSettings(ChartSettingValues values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var profile = ParseProfile(values.ThemeProfileName ?? "Dark");
        SetProfile(profile, values);
    }

    public void ApplyCustomOverrides(ChartSettingValues values)
    {
        ArgumentNullException.ThrowIfNull(values);
        SetProfile(ThemeProfile.Custom, values);
    }

    public static ThemeTokens ProfileTokens(ThemeProfile profile) => profile switch
    {
        ThemeProfile.Light => Light,
        ThemeProfile.ProDark => ProDark,
        _ => Dark
    };

    public static ThemeTokens ApplyOverrides(ThemeTokens basis, ChartSettingValues? values)
    {
        if (values is null)
            return basis;

        var grid = ParseOr(values.GridHorizontalColor, basis.GridColor);
        return new ThemeTokens
        {
            BackgroundColor = ParseOr(values.BackgroundColor, basis.BackgroundColor),
            GridColor = grid,
            GridMajorColor = grid,
            GridMinorColor = WithAlpha(grid, 0.50),
            AxisColor = ParseOr(values.AxisColor, basis.AxisColor),
            HudColor = ParseOr(values.HudTextColor, basis.HudColor),
            AccentColor = ParseOr(values.AccentColor, basis.AccentColor),
            SelectionColor = ParseOr(values.AccentColor, basis.SelectionColor),
            BullColor = ParseOr(values.BullColor, basis.BullColor),
            BearColor = ParseOr(values.BearColor, basis.BearColor),
            WarningColor = basis.WarningColor,
            ErrorColor = basis.ErrorColor,
            SuccessColor = basis.SuccessColor,
            WickColor = ParseOr(values.WickColor, basis.WickColor),
            BorderColor = ParseOr(values.BorderColor, basis.BorderColor),
            IndicatorPalette = basis.IndicatorPalette
        };
    }

    private static ThemeProfile ParseProfile(string name) => name switch
    {
        "Light" => ThemeProfile.Light,
        "ProDark" => ThemeProfile.ProDark,
        "Custom" => ThemeProfile.Custom,
        _ => ThemeProfile.Dark
    };

    private static RgbaColor ParseOr(string? hex, RgbaColor fallback)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return fallback;
        try { return RgbaColor.ParseHex(hex); }
        catch { return fallback; }
    }

    private static RgbaColor WithAlpha(RgbaColor c, double opacity)
    {
        byte a = (byte)Math.Clamp(Math.Round(255 * opacity), 0, 255);
        return RgbaColor.FromArgb(a, c.R, c.G, c.B);
    }

    private static ThemeTokens Build(
        string background, string grid, string gridMajor, string? gridMinor,
        string axis, string hud, string accent, string selection,
        string bull, string bear, string warning, string error, string success,
        string? wick = null, string? border = null)
    {
        var major = RgbaColor.ParseHex(gridMajor);
        var minor = gridMinor is null
            ? WithAlpha(major, 0.50)
            : RgbaColor.ParseHex(gridMinor);
        return new ThemeTokens
        {
            BackgroundColor = RgbaColor.ParseHex(background),
            GridColor = RgbaColor.ParseHex(grid),
            GridMajorColor = major,
            GridMinorColor = minor,
            AxisColor = RgbaColor.ParseHex(axis),
            HudColor = RgbaColor.ParseHex(hud),
            AccentColor = RgbaColor.ParseHex(accent),
            SelectionColor = RgbaColor.ParseHex(selection),
            BullColor = RgbaColor.ParseHex(bull),
            BearColor = RgbaColor.ParseHex(bear),
            WarningColor = RgbaColor.ParseHex(warning),
            ErrorColor = RgbaColor.ParseHex(error),
            SuccessColor = RgbaColor.ParseHex(success),
            WickColor = RgbaColor.ParseHex(wick ?? "#CCCCCC"),
            BorderColor = RgbaColor.ParseHex(border ?? "#1A1A1A"),
            IndicatorPalette = IndicatorPaletteColors
        };
    }
}
