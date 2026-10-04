using MyChart.Core.Models.Viewport;
using MyChart.Core.Scale;
using Xunit;

namespace MyChart.Tests.Scale;

/// <summary>T2.02 / T2.03 VERIFY = GOLDEN_TEST_VECTORS.XMapping</summary>
public class CoordinateConverterTests
{
    private static (ViewState vs, CoordinateConverter cc) Create(
        int n = 100, double plotWidth = 800, double barSpacing = 8, double rightOffset = 5)
    {
        var vs = new ViewState
        {
            Width = plotWidth + 64, // PriceAxisWidth default 64, plotLeft=0 when Right
            Height = 400,
            PriceAxisWidth = 64,
            PriceAxisSide = PriceAxisSide.Right,
            BarSpacing = barSpacing,
            RightOffset = rightOffset
        };
        // plotLeft=0, plotWidth = Width - 64 = plotWidth when Width = plotWidth+64
        var cc = new CoordinateConverter(vs, n);
        return (vs, cc);
    }

    [Fact]
    public void XMapping_GoldenVector()
    {
        var (_, cc) = Create(n: 100, plotWidth: 800, barSpacing: 8, rightOffset: 5);

        Assert.Equal(756, cc.X(99), 9);
        Assert.Equal(-36, cc.X(0), 9);
        Assert.Equal(99, cc.U(756), 9);
        Assert.Equal(4.5, cc.U(0), 9);
        Assert.Equal(104.5, cc.U(800), 9);
    }

    [Fact]
    public void RenderWindow_GoldenVector()
    {
        var (_, cc) = Create(n: 100, plotWidth: 800, barSpacing: 8, rightOffset: 5);
        var rw = RenderWindow.Compute(cc);

        Assert.Equal(5, rw.From);
        Assert.Equal(99, rw.To);
        Assert.Equal(4, rw.RenderFrom);
        Assert.Equal(99, rw.RenderTo);
        Assert.Equal(95, rw.VisibleBarCount);
    }

    [Fact]
    public void Resize_BarSpacingAndRightOffset_Unchanged()
    {
        var (vs, cc) = Create(n: 100, plotWidth: 800, barSpacing: 8, rightOffset: 5);
        // resize to plotWidth 600
        vs.Width = 600 + 64;

        Assert.Equal(8, vs.BarSpacing);
        Assert.Equal(5, vs.RightOffset);
        Assert.Equal(556, cc.X(99), 9);

        var rw = RenderWindow.Compute(cc);
        Assert.Equal(30, rw.From);
    }

    [Fact]
    public void X_U_RoundTrip()
    {
        var (_, cc) = Create();
        foreach (double x in new[] { 0.0, 100.0, 400.0, 756.0, 800.0 })
        {
            double u = cc.U(x);
            Assert.Equal(x, cc.X(u), 9);
        }
    }
}
