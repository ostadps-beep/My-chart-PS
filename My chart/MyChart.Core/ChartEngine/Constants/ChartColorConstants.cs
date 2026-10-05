namespace MyChart.Core.ChartEngine.Constants;

/// <summary>
/// Chart color defaults (T4.02 ColorTokensMinimal).
/// All values from Master Consolidated: BullColor, BearColor, GridColor, GridMajorColor,
/// GridMinorColor, AxisColor, HudColor, AccentColor, BackgroundColor, SelectionColor,
/// WarningColor, ErrorColor, SuccessColor.
/// 
/// Professional dark theme, CAD-inspired minimalist style.
/// No magic numbers; every constant here is the single source of truth.
/// </summary>
public static class ChartColorConstants
{
    /// <summary>Default hex color values for the dark theme (DECIDED_V1_1).</summary>
    public static class DarkTheme
    {
        /// <summary>Canvas background: deep dark (supports Layer 1 of render pipeline).</summary>
        public const string Background = "#1A1D23";

        /// <summary>Major grid lines: subtle gray-blue (supports Layer 2 grid rendering).</summary>
        public const string GridMajor = "#2A2E39";

        /// <summary>Minor grid lines: darker than major (optional Layer 2 detail).</summary>
        public const string GridMinor = "#16191F";

        /// <summary>Axis text and labels: high contrast white (Layer 2 axis labels).</summary>
        public const string AxisText = "#FFFFFF";

        /// <summary>Bull candle bodies and wicks: teal (professional, Layer 3).</summary>
        public const string CandleBull = "#26A69A";

        /// <summary>Bear candle bodies and wicks: professional red (Layer 3).</summary>
        public const string CandleBear = "#EF5350";

        /// <summary>Wick color: white for visibility (Layer 3).</summary>
        public const string CandleWick = "#FFFFFF";

        /// <summary>Current price marker line and direction color: matches bull (Layer 10 HUD).</summary>
        public const string CurrentPrice = "#26A69A";

        /// <summary>Crosshair cursor lines: neutral gray (Layer 7 Crosshair).</summary>
        public const string Crosshair = "#808080";

        /// <summary>HUD text and info overlay: high contrast (Layer 8 HUD).</summary>
        public const string HudText = "#FFFFFF";

        /// <summary>Selection handle and border: gold accent (Layer 6 Selection).</summary>
        public const string SelectionHandle = "#FFD700";

        /// <summary>Accent color for status and alerts: success green (Layer 10 Overlay).</summary>
        public const string Accent = "#4CAF50";

        /// <summary>Fully transparent overlay base (Layer 10 when needed).</summary>
        public const string TransparentBg = "#00000000";

        /// <summary>Warning/caution color (future use, Layer 10).</summary>
        public const string Warning = "#FF9800";

        /// <summary>Error color (future use, Layer 10).</summary>
        public const string Error = "#F44336";
    }

    /// <summary>Opacity constants (as percentages for use with RgbaColor).</summary>
    public static class Opacity
    {
        /// <summary>Grid lines: 40% opacity per Master Consolidated (CurrentPriceAndDualMarkerValues).</summary>
        public const double GridLineOpacity = 0.40;

        /// <summary>HUD overlay: approximately 75% opacity per Master Consolidated.</summary>
        public const double HudOverlayOpacity = 0.75;

        /// <summary>Crosshair dashed line: 40% opacity per Master Consolidated (T2.10).</summary>
        public const double CrosshairOpacity = 0.40;

        /// <summary>Full opacity (no transparency).</summary>
        public const double Opaque = 1.0;

        /// <summary>Fully transparent.</summary>
        public const double Transparent = 0.0;
    }
}
