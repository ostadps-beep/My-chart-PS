using MyChart.Core.ChartEngine.Constants;
using MyChart.Core.Contracts.Services;

namespace MyChart.Core.Models.Rendering;

/// <summary>
/// Theme palette factory and manager (T4.02 ColorTokensMinimal).
/// 
/// Responsibilities:
/// - Parse hex color strings from constants into RgbaColor structs
/// - Create immutable ChartColorTokens instances
/// - Support default (dark) theme
/// - Integration point for future settings (T6.08)
/// 
/// RULE: Palettes are created once per render session and cached.
/// RULE: No modification after creation; all tokens are immutable.
/// RULE: Hex parsing errors are caught and reported; invalid colors fail loudly.
/// </summary>
public sealed class ThemePalette
{
    /// <summary>The immutable color tokens for this palette.</summary>
    public ChartColorTokens Tokens { get; }

    /// <summary>
    /// Private constructor ensures palettes are created via factory methods only.
    /// </summary>
    private ThemePalette(ChartColorTokens tokens)
    {
        Tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
    }

    /// <summary>
    /// Create the default dark theme palette (DECIDED_V1_1).
    /// All colors parsed from ChartColorConstants.DarkTheme hex values.
    /// </summary>
    /// <returns>A new immutable ThemePalette with dark theme colors.</returns>
    /// <exception cref="FormatException">If any hex color cannot be parsed.</exception>
    public static ThemePalette CreateDefault()
    {
        var tokens = new ChartColorTokens(
            Background: RgbaColor.FromHex(ChartColorConstants.DarkTheme.Background),
            GridMajor: RgbaColor.FromHex(ChartColorConstants.DarkTheme.GridMajor),
            GridMinor: RgbaColor.FromHex(ChartColorConstants.DarkTheme.GridMinor),
            AxisText: RgbaColor.FromHex(ChartColorConstants.DarkTheme.AxisText),
            CandleBull: RgbaColor.FromHex(ChartColorConstants.DarkTheme.CandleBull),
            CandleBear: RgbaColor.FromHex(ChartColorConstants.DarkTheme.CandleBear),
            CandleWick: RgbaColor.FromHex(ChartColorConstants.DarkTheme.CandleWick),
            CurrentPrice: RgbaColor.FromHex(ChartColorConstants.DarkTheme.CurrentPrice),
            Crosshair: RgbaColor.FromHex(ChartColorConstants.DarkTheme.Crosshair),
            HudText: RgbaColor.FromHex(ChartColorConstants.DarkTheme.HudText),
            SelectionHandle: RgbaColor.FromHex(ChartColorConstants.DarkTheme.SelectionHandle),
            Accent: RgbaColor.FromHex(ChartColorConstants.DarkTheme.Accent),
            TransparentBg: RgbaColor.FromHex(ChartColorConstants.DarkTheme.TransparentBg),
            Warning: RgbaColor.FromHex(ChartColorConstants.DarkTheme.Warning),
            Error: RgbaColor.FromHex(ChartColorConstants.DarkTheme.Error)
        );
        return new ThemePalette(tokens);
    }

    /// <summary>
    /// Create a palette from settings (integration point for T6.08 Settings panel).
    /// 
    /// Currently falls back to default; when Settings panel is integrated (T6.08),
    /// this method will read color overrides from IChartSettings and apply them.
    /// </summary>
    /// <param name="settings">Chart settings service (read-only).</param>
    /// <returns>A new palette, respecting any user settings.</returns>
    public static ThemePalette FromSettings(IChartSettings settings)
    {
        // T6.08: Parse settings.Values and apply overrides
        // For now, use default palette; Settings panel is not yet integrated.
        ArgumentNullException.ThrowIfNull(settings);
        return CreateDefault();
    }
}
