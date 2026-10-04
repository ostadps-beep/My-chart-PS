using MyChart.Core.Models.Viewport;

namespace MyChart.Core.Scale;

/// <summary>
/// T2.05 PanEngineAndScrollLimits — pure pan math.
/// </summary>
public static class PanEngine
{
    public const double DragThresholdDip = 3;

    /// <summary>dx pixels → RightOffset += dx / BarSpacing (drag right = older bars).</summary>
    public static void PanHorizontal(ViewState vs, int n, double dxDip)
    {
        if (vs.BarSpacing <= 0) return;
        vs.RightOffset += dxDip / vs.BarSpacing;
        vs.RightOffset = ClampRightOffset(vs, n);
    }

    /// <summary>
    /// RightOffset &lt;= plotWidth/BarSpacing - 2 ;
    /// RightOffset &gt;= 2 - N
    /// </summary>
    public static double ClampRightOffset(ViewState vs, int n)
    {
        double upper = vs.PlotWidth / vs.BarSpacing - 2;
        double lower = 2 - n;
        return Math.Clamp(vs.RightOffset, lower, upper);
    }

    public static double MaxRightOffset(ViewState vs)
        => vs.PlotWidth / vs.BarSpacing - 2;

    public static double MinRightOffset(int n)
        => 2 - n;

    /// <summary>
    /// Following = (-0.5 &lt;= RightOffset &lt;= plotWidth/BarSpacing - 0.5)
    /// </summary>
    public static bool IsFollowing(ViewState vs)
    {
        double upper = vs.PlotWidth / vs.BarSpacing - 0.5;
        return vs.RightOffset >= -0.5 && vs.RightOffset <= upper;
    }

    /// <summary>
    /// APPEND_RULE: if Following, RightOffset unchanged; else RightOffset -= 1.
    /// </summary>
    public static void OnBarAppended(ViewState vs, bool autoScroll = true)
    {
        if (!autoScroll) return; // Following forced false path: still apply -1
        if (IsFollowing(vs))
            return;
        vs.RightOffset -= 1;
    }

    /// <summary>PREPEND_RULE: RightOffset unchanged.</summary>
    public static void OnBarsPrepended(ViewState vs, int count)
    {
        // intentionally no-op
    }
}
