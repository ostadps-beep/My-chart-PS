namespace MyChart.Core.ChartEngine.Constants;

/// <summary>
/// Rendering constants (T4.01 and T4.02).
/// Device-independent pixel (DIP) conversions and default text styling.
/// </summary>
public static class RenderConstants
{
    /// <summary>Default text styling for axis labels and UI.</summary>
    public static class Text
    {
        /// <summary>Default axis label font (platform monospace).</summary>
        public const string DefaultFont = "Consolas";

        /// <summary>Default axis label size in DIP.</summary>
        public const double DefaultSizeDip = 11.0;

        /// <summary>Axis labels are never bold (per spec).</summary>
        public const bool DefaultBold = false;
    }

    /// <summary>Rendering performance tuning.</summary>
    public static class Performance
    {
        /// <summary>Maximum number of cached paint objects (per type).</summary>
        public const int MaxCachedPaints = 256;

        /// <summary>Maximum number of cached text paints.</summary>
        public const int MaxCachedTextPaints = 128;
    }

    /// <summary>Stroke and line styling.</summary>
    public static class Stroke
    {
        /// <summary>Default line width in DIP for grid and crosshair.</summary>
        public const double DefaultWidthDip = 1.0;

        /// <summary>Default dash pattern for current price marker (SkiaSharp format: on/off cycle).</summary>
        public static readonly float[] DefaultDashPattern = { 4f, 4f }; // 4px on, 4px off
    }
}
