using MyChart.Core.Models.Viewport;
using MyChart.Core.Scale;
using Xunit;

namespace MyChart.Tests.Scale;

/// <summary>T2.04 / T2.05 VERIFY = GOLDEN_TEST_VECTORS.Zoom, Pan</summary>
public class ZoomPanTests
{
    private static (ViewState vs, CoordinateConverter cc) Setup(
        int n = 100, double plotWidth = 800, double barSpacing = 8, double rightOffset = 5)
    {
        var vs = new ViewState
        {
            Width = plotWidth + 64,
            Height = 400,
            PriceAxisWidth = 64,
            PriceAxisSide = PriceAxisSide.Right,
            BarSpacing = barSpacing,
            RightOffset = rightOffset
        };
        return (vs, new CoordinateConverter(vs, n));
    }

    [Fact]
    public void Zoom_OneNotch_Golden()
    {
        var (vs, cc) = Setup();
        double cursorX = 400;
        Assert.Equal(54.5, cc.U(cursorX), 9);

        ZoomEngine.ZoomAt(vs, 100, cursorX, ZoomEngine.ZoomFactor(), notches: 1);

        Assert.Equal(8.8, vs.BarSpacing, 9);
        // golden: 0.454545 tolerance 1e-6
        Assert.True(Math.Abs(vs.RightOffset - 0.45454545454545) < 1e-6,
            $"RightOffset={vs.RightOffset}");

        var cc2 = new CoordinateConverter(vs, 100);
        Assert.Equal(54.5, cc2.U(cursorX), 6);
    }

    [Fact]
    public void Zoom_Precision_CtrlWheel_Golden()
    {
        var (vs, _) = Setup();
        double factor = ZoomEngine.PrecisionZoomFactor(); // 1.02
        Assert.Equal(1.02, factor, 9);

        ZoomEngine.ZoomAt(vs, 100, 400, factor, notches: 1);

        Assert.Equal(8.16, vs.BarSpacing, 9);
        Assert.True(Math.Abs(vs.RightOffset - 4.01960784313725) < 1e-5,
            $"RightOffset={vs.RightOffset}");
    }

    [Fact]
    public void Zoom_ClampsToMaxBarSpacing()
    {
        var (vs, _) = Setup(barSpacing: 49.9);
        ZoomEngine.ZoomAt(vs, 100, 400, 1.10, notches: 1);
        Assert.Equal(50, vs.BarSpacing, 9);
    }

    [Fact]
    public void Pan_DragRight40_Golden()
    {
        var (vs, _) = Setup(rightOffset: 5, barSpacing: 8);
        PanEngine.PanHorizontal(vs, 100, dxDip: 40);
        Assert.Equal(10, vs.RightOffset, 9);
    }

    [Fact]
    public void Pan_ScrollLimits_Golden()
    {
        var (vs, _) = Setup(n: 100, plotWidth: 800, barSpacing: 8);

        Assert.Equal(98, PanEngine.MaxRightOffset(vs), 9);
        Assert.Equal(-98, PanEngine.MinRightOffset(100), 9);

        vs.RightOffset = 130;
        vs.RightOffset = PanEngine.ClampRightOffset(vs, 100);
        Assert.Equal(98, vs.RightOffset, 9);
    }
}
