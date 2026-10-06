using MyChart.Core.Contracts.UI;
using MyChart.Core.Models.Rendering;

namespace MyChart.Core.Services;

/// <summary>
/// T4.02 ColorTokensMinimal. Tier 4 ships ONLY the Dark profile (T7.02 values).
/// Light, ProDark, Custom come in T7.02.
/// </summary>
public sealed class ThemeService : IThemeService
{
    // Must be initialized BEFORE Dark/ProDark static properties (declaration order).
    private static readonly RgbaColor[] IndicatorPaletteColors =
    {
        RgbaColor.ParseHex("#2962FF"),
        RgbaColor.ParseHex("#FF9800"),
        RgbaColor.ParseHex("#E040FB"),
        RgbaColor.ParseHex("#00BCD4"),
        RgbaColor.ParseHex("#FFEB3B"),
        RgbaColor.ParseHex("#8BC34A"),
    };

    public static ThemeTokens Dark { get; } = Build(
        background: "#131722",
        grid: "#1E222D",
        gridMajor: "#2A2E39",
        gridMinor: "#1E222D",
        axis: "#787B86",
        hud: "#B2B5BE",
        accent: "#2962FF",
        selection: "#2962FF",
        bull: "#26A69A",
        bear: "#EF5350",
        warning: "#FF9800",
        error: "#F23645",
        success: "#089981");

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

    public ThemeService(string profileName = "Dark")
    {
        ProfileName = profileName;
        Current = profileName switch
        {
            "ProDark" => ProDark,
            _ => Dark
        };
    }

    public string ProfileName { get; }
    public ThemeTokens Current { get; }

    private static ThemeTokens Build(
        string background, string grid, string gridMajor, string gridMinor,
        string axis, string hud, string accent, string selection,
        string bull, string bear, string warning, string error, string success)
        => new()
        {
            BackgroundColor = RgbaColor.ParseHex(background),
            GridColor = RgbaColor.ParseHex(grid),
            GridMajorColor = RgbaColor.ParseHex(gridMajor),
            GridMinorColor = RgbaColor.ParseHex(gridMinor),
            AxisColor = RgbaColor.ParseHex(axis),
            HudColor = RgbaColor.ParseHex(hud),
            AccentColor = RgbaColor.ParseHex(accent),
            SelectionColor = RgbaColor.ParseHex(selection),
            BullColor = RgbaColor.ParseHex(bull),
            BearColor = RgbaColor.ParseHex(bear),
            WarningColor = RgbaColor.ParseHex(warning),
            ErrorColor = RgbaColor.ParseHex(error),
            SuccessColor = RgbaColor.ParseHex(success),
            WickColor = RgbaColor.ParseHex("#CCCCCC"),
            BorderColor = RgbaColor.ParseHex("#1A1A1A"),
            IndicatorPalette = IndicatorPaletteColors
        };
}
