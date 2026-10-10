using MyChart.Core.Models.Viewport;

namespace MyChart.Core.Scale;

/// <summary>
/// T2.04 ZoomEngine — pure zoom math. Anchor keeps u under cursor fixed.
/// Supports fractional notch amounts for smooth wheel zoom.
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
    /// Zoom around cursorX. amount &gt; 0 = zoom in (larger BarSpacing).
    /// amount may be fractional (smooth wheel).
    /// </summary>
    public static void ZoomAt(
        ViewState vs,
        int n,
        double cursorX,
        double factor,
        double amount = 1.0)
    {
        if (n <= 0 || vs.BarSpacing <= 0 || amount == 0) return;

        var cc = new CoordinateConverter(vs, n);
        double u0 = cc.U(cursorX);

        double target = vs.BarSpacing * Math.Pow(factor, amount);
        target = Math.Clamp(target, MinBarSpacing, MaxBarSpacing);

        vs.BarSpacing = target;

        vs.RightOffset = u0 - (n - 1) - 0.5
                         + (vs.PlotLeft + vs.PlotWidth - cursorX) / vs.BarSpacing;
    }

    /// <summary>Integer notch overload for golden tests.</summary>
    public static void ZoomAt(
        ViewState vs,
        int n,
        double cursorX,
        double factor,
        int notches)
        => ZoomAt(vs, n, cursorX, factor, (double)notches);

    public static double SmoothStep(double current, double target, double dtSeconds)
    {
        double diff = target - current;
        if (Math.Abs(diff) < 0.01) return target;
        return current + diff * (1.0 - Math.Exp(-dtSeconds / 0.050));
    }
}
