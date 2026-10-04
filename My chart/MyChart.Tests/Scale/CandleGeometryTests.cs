using MyChart.Core.Models.Market;
using MyChart.Core.Models.Viewport;
using MyChart.Core.Scale;
using Xunit;

namespace MyChart.Tests.Scale;

public class CandleGeometryTests
{
    private static CoordinateConverter Cc(double barSpacing = 8, int n = 10)
    {
        var vs = new ViewState
        {
            Width = 800 + 64,
            Height = 400,
            BarSpacing = barSpacing,
            RightOffset = 0,
            PriceScale = new PriceScaleState { MinPrice = 1.09, MaxPrice = 1.12 }
        };
        return new CoordinateConverter(vs, n);
    }

    [Fact]
    public void Mode_BySpacingDev()
    {
        Assert.Equal(CandleDrawMode.FullCandle, CandleGeometryCalculator.ModeFor(8, 1));
        Assert.Equal(CandleDrawMode.WickOnly, CandleGeometryCalculator.ModeFor(2.5, 1));
        Assert.Equal(CandleDrawMode.PixelColumn, CandleGeometryCalculator.ModeFor(1, 1));
        Assert.Equal(CandleDrawMode.WickOnly, CandleGeometryCalculator.ModeFor(8, 1, showBody: false));
    }

    [Fact]
    public void FullCandle_IntegerOutputs_Doji()
    {
        var cc = Cc();
        var candle = new Candle(DateTimeOffset.UtcNow, 1.10, 1.11, 1.09, 1.10, 1); // doji O==C
        var g = CandleGeometryCalculator.Compute(candle, 5, cc, dpiScale: 1.0, digits: 5);

        Assert.Equal(CandleDrawMode.FullCandle, g.Mode);
        Assert.True(g.IsDoji);
        Assert.Equal(1, g.BodyHeight);
        Assert.True(g.IsBull); // Close >= Open
        Assert.True(g.YLow >= g.YHigh);
    }

    [Fact]
    public void BodyWidth_Odd_And_Gap()
    {
        var cc = Cc(barSpacing: 8);
        var candle = new Candle(DateTimeOffset.UtcNow, 1.10, 1.11, 1.09, 1.105, 1);
        var g = CandleGeometryCalculator.Compute(candle, 5, cc, dpiScale: 1.0);
        // spacingDev=8, gap=2 → floor(6)=6 even → 5; max floor(8)-1=7 → 5
        Assert.Equal(5, g.BodyWidth);
        Assert.True(g.BodyWidth % 2 == 1 || g.BodyWidth == 1);
    }
}
