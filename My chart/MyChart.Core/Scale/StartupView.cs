using MyChart.Core.Models.Viewport;

namespace MyChart.Core.Scale;

/// <summary>T2.11 StartupView — initial BarSpacing, RightOffset, Fit.</summary>
public static class StartupView
{
    public const int DefaultVisibleCandles = 51;
    public const double DefaultBarSpacing = 8;

    /// <summary>
    /// BarSpacing = clamp(plotWidth * (shift ? 0.5 : 1) / visibleCandles, 0.5, 50)
    /// RightOffset = shift ? plotWidth/(2*BarSpacing)-0.5 : 0
    /// </summary>
    public static void Apply(
        ViewState vs,
        int visibleCandles = DefaultVisibleCandles,
        bool shift = true)
    {
        if (visibleCandles <= 0) visibleCandles = DefaultVisibleCandles;
        double factor = shift ? 0.5 : 1.0;
        double spacing = vs.PlotWidth * factor / visibleCandles;
        vs.BarSpacing = Math.Clamp(spacing, ZoomEngine.MinBarSpacing, ZoomEngine.MaxBarSpacing);

        if (shift)
            vs.RightOffset = vs.PlotWidth / (2.0 * vs.BarSpacing) - 0.5;
        else
            vs.RightOffset = 0;

        vs.PriceScale.Fit = ScaleFit.Auto;
        vs.PriceScale.TransformKind = ScaleTransformKind.Linear;
    }

    public static void ResetView(ViewState vs, int visibleCandles = DefaultVisibleCandles, bool shift = true)
        => Apply(vs, visibleCandles, shift);
}
