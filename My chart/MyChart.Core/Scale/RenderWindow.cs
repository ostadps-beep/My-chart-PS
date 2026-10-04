using MyChart.Core.Models.Viewport;

namespace MyChart.Core.Scale;

/// <summary>
/// T2.03 RenderWindow — from/to/renderFrom/renderTo/VisibleBarCount.
/// </summary>
public static class RenderWindow
{
    public static RenderWindowRange Compute(CoordinateConverter cc)
    {
        int n = cc.N;
        if (n == 0)
            return new RenderWindowRange(0, -1, 0, -1, 0);

        var vs = cc.ViewState;
        double iLeft = cc.U(vs.PlotLeft);
        double iRight = cc.U(vs.PlotLeft + vs.PlotWidth);

        int from = Math.Max(0, (int)Math.Floor(iLeft + 0.5));
        int to = Math.Min(n - 1, (int)Math.Ceiling(iRight + 0.5) - 1);

        if (from > to)
            return new RenderWindowRange(from, to, from, to, 0);

        int renderFrom = Math.Max(0, from - 1);
        int renderTo = Math.Min(n - 1, to + 1);
        int visible = to - from + 1;

        return new RenderWindowRange(from, to, renderFrom, renderTo, visible);
    }
}
