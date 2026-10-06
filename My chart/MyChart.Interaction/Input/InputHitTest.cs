using MyChart.Core.Models.Viewport;

namespace MyChart.Interaction.Input;

public static class InputHitTest
{
    public static HitRegion RegionAt(ViewState vs, double x, double y)
    {
        if (x < 0 || y < 0 || x > vs.Width || y > vs.Height)
            return HitRegion.Outside;

        double plotBottom = vs.PlotTop + vs.PlotHeight;
        double plotRight = vs.PlotRight;

        // Price axis (right by default)
        if (vs.PriceAxisSide == PriceAxisSide.Right)
        {
            if (x >= plotRight && x <= vs.Width && y >= vs.PlotTop && y <= plotBottom)
                return HitRegion.PriceAxis;
        }
        else
        {
            if (x >= 0 && x <= vs.PriceAxisWidth && y >= vs.PlotTop && y <= plotBottom)
                return HitRegion.PriceAxis;
        }

        // Time axis at bottom
        if (y > plotBottom && y <= vs.Height && x >= vs.PlotLeft && x <= plotRight)
            return HitRegion.TimeAxis;

        if (x >= vs.PlotLeft && x <= plotRight && y >= vs.PlotTop && y <= plotBottom)
            return HitRegion.Plot;

        return HitRegion.Outside;
    }
}
