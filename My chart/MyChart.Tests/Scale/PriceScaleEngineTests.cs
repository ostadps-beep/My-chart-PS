using MyChart.Core.Models.Market;
using MyChart.Core.Models.Viewport;
using MyChart.Core.Scale;
using Xunit;

namespace MyChart.Tests.Scale;

public class PriceScaleEngineTests
{
    private static List<Candle> Bars()
    {
        var t0 = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        return new List<Candle>
        {
            new(t0, 1.1000, 1.1050, 1.0950, 1.1020, 1),
            new(t0.AddMinutes(1), 1.1020, 1.1080, 1.1000, 1.1060, 1),
            new(t0.AddMinutes(2), 1.1060, 1.1100, 1.1040, 1.1090, 1),
        };
    }

    [Fact]
    public void Auto_PadsTenPercent_Linear()
    {
        var state = new PriceScaleState { Fit = ScaleFit.Auto, Transform = ScaleTransformKind.Linear };
        var bars = Bars();
        // hi=1.1100 lo=1.0950 span=0.015; pad 10% each of spanT=0.015 → ±0.0015
        PriceScaleEngine.ComputeAuto(state, bars, 0, 2, pointSize: 0.00001);

        Assert.True(state.MaxPrice > 1.1100);
        Assert.True(state.MinPrice < 1.0950);
        double span = 1.1100 - 1.0950;
        Assert.Equal(1.1100 + span * 0.10, state.MaxPrice, 9);
        Assert.Equal(1.0950 - span * 0.10, state.MinPrice, 9);
    }

    [Fact]
    public void Log_FallsBack_WhenLowNonPositive()
    {
        var state = new PriceScaleState { Fit = ScaleFit.Auto, Transform = ScaleTransformKind.Log };
        var t0 = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var bars = new List<Candle>
        {
            new(t0, 1, 1, 0, 1, 1) // Low = 0
        };
        PriceScaleEngine.ComputeAuto(state, bars, 0, 0, 0.01);
        Assert.Equal(ScaleTransformKind.Linear, state.Transform);
        Assert.True(state.LogUnavailable);
    }

    [Fact]
    public void SetManual_Locks()
    {
        var state = new PriceScaleState();
        PriceScaleEngine.SetManual(state, 1.0, 2.0);
        Assert.Equal(ScaleFit.Manual, state.Fit);
        Assert.True(state.IsManualLocked);
        Assert.Equal(1.0, state.MinPrice, 9);
        Assert.Equal(2.0, state.MaxPrice, 9);
    }

    [Fact]
    public void BackToAuto_ClearsLock()
    {
        var state = new PriceScaleState();
        PriceScaleEngine.SetManual(state, 1, 2);
        PriceScaleEngine.SetAuto(state);
        Assert.Equal(ScaleFit.Auto, state.Fit);
        Assert.False(state.IsManualLocked);
    }
}
