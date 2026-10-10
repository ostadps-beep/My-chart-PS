namespace MyChart.Core.Models.Settings;

/// <summary>Typed chart settings. Defaults match C1 / SettingsSchema.</summary>
public sealed class ChartSettingValues
{
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

    public bool ShowGrid { get; set; } = true;
    public string GridHorizontalColor { get; set; } = "#2A2E39";
    public string GridStyle { get; set; } = "Solid";
    public bool ShowHorizontalGrid { get; set; } = true;
    public bool ShowVerticalGrid { get; set; } = true;
    public int GridTransparency { get; set; } = 40;
    public string PriceAxisPosition { get; set; } = "Right";
    public bool ShowLastPrice { get; set; } = true;
    public string AxisColor { get; set; } = "#888888";

    public double CandlesSpacing { get; set; } = 1.0;
    public string BackgroundColor { get; set; } = "#1E1E1E";
    public string BullColor { get; set; } = "#26A69A";
    public string BearColor { get; set; } = "#EF5350";
    public string WickColor { get; set; } = "#CCCCCC";
    public string BorderColor { get; set; } = "#1A1A1A";
    public string AccentColor { get; set; } = "#2962FF";
    public string ThemeProfileName { get; set; } = "Dark";

    public string HudItems { get; set; } = "OHLC, Change, Volume";
    public string HudPosition { get; set; } = "TopLeft";
    public string HudTextColor { get; set; } = "#FFFFFF";
    public int HudFontSize { get; set; } = 12;
    public string HudFontType { get; set; } = "Segoe UI";
    public int HudTransparency { get; set; } = 0;

    public int FpsLimit { get; set; } = 60;
    public bool AntiAliasing { get; set; } = true;
    public string LogLevel { get; set; } = "Info";
    public string ErrorBehavior { get; set; } = "Continue";

    public bool DrawingShowLabels { get; set; } = true;
}
