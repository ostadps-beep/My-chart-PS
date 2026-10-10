using MyChart.Core.Models.Market;
using MyChart.Core.Models.Viewport;
using MyChart.Core.Scale;
using Xunit;

namespace MyChart.Tests.Viewport;

/// <summary>C2 — Manual price scale + anchor-fixed zoom.</summary>
public class PriceScaleFitPolicyTests
{
    private static List<Candle> Bars(int n, double basePrice = 1.10)
    {
        var list = new List<Candle>(n);
        var t0 = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        for (int i = 0; i < n; i++)
        {
            double o = basePrice + i * 0.0001;
            list.Add(new Candle(t0.AddMinutes(i), o, o + 0.0005, o - 0.0005, o + 0.0002, 100));
        }
        return list;
    }

    [Fact]
    public void ComputeAuto_DoesNothing_WhenManual()
    {
        var state = new PriceScaleState();
        PriceScaleEngine.SetManual(state, minT: 1.0, maxT: 2.0);
        var bars = Bars(50);
        var minBefore = state.MinPrice;
        var maxBefore = state.MaxPrice;

        PriceScaleEngine.ComputeAuto(state, bars, 0, 49, pointSize: 0.00001);

        Assert.Equal(ScaleFit.Manual, state.Fit);
        Assert.Equal(minBefore, state.MinPrice);
        Assert.Equal(maxBefore, state.MaxPrice);
    }

    [Fact]
    public void ComputeAuto_UpdatesRange_WhenAuto()
    {
        var state = new PriceScaleState();
        PriceScaleEngine.SetAuto(state);
        state.MinPrice = 0;
        state.MaxPrice = 100;
        var bars = Bars(40, basePrice: 1.20);

        PriceScaleEngine.ComputeAuto(state, bars, 0, 39, pointSize: 0.00001);

        Assert.Equal(ScaleFit.Auto, state.Fit);
        Assert.True(state.MinPrice < state.MaxPrice);
        Assert.InRange(state.MinPrice, 1.0, 1.25);
        Assert.InRange(state.MaxPrice, 1.15, 1.40);
    }

    [Fact]
    public void ZoomAroundPrice_KeepsAnchorInsideRange()
    {
        var state = new PriceScaleState();
        PriceScaleEngine.SetManual(state, 1.0, 2.0);
        double anchor = 1.4;

        PriceScaleEngine.ZoomAroundPrice(state, anchor, factor: 0.5); // zoom in

        Assert.Equal(ScaleFit.Manual, state.Fit);
        Assert.True(state.MinPrice < anchor && anchor < state.MaxPrice);
        // span halved
        Assert.InRange(state.MaxPrice - state.MinPrice, 0.49, 0.51);
    }

    [Fact]
    public void ZoomAroundPrice_AnchorStaysSameRelativePosition()
    {
        var state = new PriceScaleState();
        PriceScaleEngine.SetManual(state, 1.0, 2.0);
        double anchor = 1.25; // 25% from min
        double fracBefore = (anchor - 1.0) / (2.0 - 1.0);

        PriceScaleEngine.ZoomAroundPrice(state, anchor, factor: 2.0); // zoom out

        double fracAfter = (anchor - state.MinPrice) / (state.MaxPrice - state.MinPrice);
        Assert.InRange(fracAfter, fracBefore - 0.001, fracBefore + 0.001);
    }

    [Fact]
    public void SetManual_LocksFit()
    {
        var state = new PriceScaleState();
        PriceScaleEngine.SetAuto(state);
        PriceScaleEngine.SetManual(state, 10, 20);
        Assert.Equal(ScaleFit.Manual, state.Fit);
        Assert.True(state.IsManualLocked);
    }
}
