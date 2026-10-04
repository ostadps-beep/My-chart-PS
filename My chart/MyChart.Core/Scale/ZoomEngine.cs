using MyChart.Core.Models.Viewport;

namespace MyChart.Core.Scale;

/// <summary>
/// T2.04 ZoomEngine — pure zoom math. Anchor keeps u under cursor fixed.
/// MinBarSpacing=0.5, MaxBarSpacing=50.
/// </summary>
public static class ZoomEngine
{
    public const double MinBarSpacing = 0.5;
    public const double MaxBarSpacing = 50.0;
    public const double DefaultZoomSpeed = 50; // -> factor 1.10

    public static double ZoomFactor(double speed = DefaultZoomSpeed)
        => 1.0 + speed * 0.002;

    public static double PrecisionZoomFactor(double speed = DefaultZoomSpeed)
    {
        double f = ZoomFactor(speed);
        return 1.0 + (f - 1.0) * 0.2;
    }

    /// <summary>
    /// Apply zoom in/out around cursorX. notches &gt; 0 = zoom in.
    /// Updates BarSpacing and RightOffset on <paramref name="vs"/>.
    /// </summary>
    public static void ZoomAt(
        ViewState vs,
        int n,
        double cursorX,
        double factor,
        int notches = 1)
    {
        if (n <= 0 || vs.BarSpacing <= 0) return;

        var cc = new CoordinateConverter(vs, n);
        double u0 = cc.U(cursorX);

        double target = vs.BarSpacing * Math.Pow(factor, notches);
        target = Math.Clamp(target, MinBarSpacing, MaxBarSpacing);

        vs.BarSpacing = target;

        // RightOffset' = u0 - (N-1) - 0.5 + (plotLeft + plotWidth - cursorX) / BarSpacing'
        vs.RightOffset = u0 - (n - 1) - 0.5
                         + (vs.PlotLeft + vs.PlotWidth - cursorX) / vs.BarSpacing;
    }

    /// <summary>Smooth step toward targetSpacing; returns new spacing.</summary>
    public static double SmoothStep(double current, double target, double dtSeconds)
    {
        double diff = target - current;
        if (Math.Abs(diff) < 0.01) return target;
        return current + diff * (1.0 - Math.Exp(-dtSeconds / 0.050));
    }
}
