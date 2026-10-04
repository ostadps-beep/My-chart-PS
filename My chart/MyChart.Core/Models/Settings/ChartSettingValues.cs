namespace MyChart.Core.Models.Settings;

/// <summary>
/// Typed chart settings. Defaults match SETTINGS_KEY_MAP.
/// Until T6.08 the chart uses DefaultChartSettings; the real panel is not in the solution yet.
/// </summary>
public sealed class ChartSettingValues
{
    // --- chart ---
    public bool Offline { get; set; } = false;
    public bool Shift { get; set; } = true;
    public bool Autoscroll { get; set; } = true;
    public bool ScaleFix { get; set; } = false;
    public double ScaleFixedMinimum { get; set; } = 0;
    public double ScaleFixedMaximum { get; set; } = 0;
    public string ZoomBehavior { get; set; } = "Both";
    public string MouseWheel { get; set; } = "Zoom";
    public int ZoomSpeed { get; set; } = 50;
    public int ScrollSpeed { get; set; } = 50;
    public int VisibleCandles { get; set; } = 500;
    public string DisplayMode { get; set; } = "Candlesticks";
    public bool ShowOhlc { get; set; } = true;

    // --- grid / axes (subset with chart consumers) ---
    public bool ShowGrid { get; set; } = true;
    public string GridHorizontalColor { get; set; } = "#2A2E39";
    public string GridStyle { get; set; } = "Solid";
    public bool ShowHorizontalGrid { get; set; } = true;
    public bool ShowVerticalGrid { get; set; } = true;
    public int GridTransparency { get; set; } = 40;
    public string PriceAxisPosition { get; set; } = "Right";
    public bool ShowLastPrice { get; set; } = true;

    // --- candles ---
    public double CandlesSpacing { get; set; } = 1.0; // body width follows candles.spacing

    // --- hud ---
    public string HudItems { get; set; } = "OHLC, Change, Volume";
    public string HudPosition { get; set; } = "TopLeft";
    public string HudTextColor { get; set; } = "#FFFFFF";
    public int HudFontSize { get; set; } = 12;
    public string HudFontType { get; set; } = "Segoe UI";
    public int HudTransparency { get; set; } = 0;

    // --- performance ---
    public int FpsLimit { get; set; } = 60;
    public bool AntiAliasing { get; set; } = true;
    public string LogLevel { get; set; } = "Info";
    public string ErrorBehavior { get; set; } = "Continue";

    // --- drawing tools (subset) ---
    public bool DrawingShowLabels { get; set; } = true;
}
