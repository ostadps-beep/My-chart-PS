using MyChart.Core.Models.Market;
using MyChart.Core.Models.Viewport;
using MyChart.Core.Scale;
using Xunit;

namespace MyChart.Tests.Scale;

public class CurrentPriceAndStartupTests
{
    [Fact]
    public void CurrentPrice_PrefersTickBid()
    {
        var candle = new Candle(DateTimeOffset.UtcNow, 1, 1, 1, 1.5, 1);
        Assert.Equal(1.2, CurrentPriceValues.ResolveCurrentPrice(1.2, candle));
        Assert.Equal(1.5, CurrentPriceValues.ResolveCurrentPrice(null, candle));
    }

    [Fact]
    public void Countdown_Format()
    {
        Assert.Equal("5:03", CurrentPriceValues.FormatCountdown(TimeSpan.FromSeconds(5 * 60 + 3)));
        Assert.Equal("2:05:09", CurrentPriceValues.FormatCountdown(TimeSpan.FromHours(2) + TimeSpan.FromMinutes(5) + TimeSpan.FromSeconds(9)));
        Assert.Equal("3d 04:00", CurrentPriceValues.FormatCountdown(TimeSpan.FromDays(3) + TimeSpan.FromHours(4)));
    }

    [Fact]
    public void Countdown_AtBoundary_IsZero()
    {
        var cal = new SessionCalendar(ServerTimeRule.Utc, SymbolGroup.Forex);
        var open = new DateTimeOffset(2024, 3, 13, 12, 0, 0, TimeSpan.Zero);
        var next = open.AddMinutes(1);
        var remaining = CurrentPriceValues.Countdown(open, Timeframe.M1, cal, next, TimeSpan.Zero);
        Assert.Equal(TimeSpan.Zero, remaining);
        Assert.Equal("0:00", CurrentPriceValues.FormatCountdown(remaining));
    }

    [Fact]
    public void ClampMarkerY()
    {
        var vs = new ViewState { Width = 864, Height = 424, PriceAxisWidth = 64, TimeAxisHeight = 24 };
        var (y, clamped) = CurrentPriceValues.ClampMarkerY(-10, vs);
        Assert.True(clamped);
        Assert.Equal(0, y);
        (y, clamped) = CurrentPriceValues.ClampMarkerY(10000, vs);
        Assert.True(clamped);
        Assert.Equal(vs.PlotHeight, y);
    }

    [Fact]
    public void StartupView_CentresLastBar_AndVisibleCount()
    {
        var vs = new ViewState
        {
            Width = 800 + 64,
            Height = 400,
            PriceAxisWidth = 64
        };
        // shift=true uses half formula → spacing = 800*0.5/51 ≈ 7.84
        StartupView.Apply(vs, visibleCandles: 51, shift: true);

        int n = 100;
        var cc = new CoordinateConverter(vs, n);
        double xLast = cc.X(n - 1);
        Assert.Equal(vs.PlotLeft + vs.PlotWidth / 2.0, xLast, 6);

        var rw = RenderWindow.Compute(cc);
        Assert.Equal(n - 1, rw.To);
        // with centred last bar and ~51 visible, from ≈ N-51
        Assert.True(rw.VisibleBarCount >= 50 && rw.VisibleBarCount <= 52,
            $"VisibleBarCount={rw.VisibleBarCount}");
    }
}
