using MyChart.Core.Candles;
using MyChart.Core.Models.Market;
using Xunit;

namespace MyChart.Tests.Market;

/// <summary>
/// T1.06 VERIFY = GOLDEN_TEST_VECTORS.TicksToM1, FoldM5
/// </summary>
public class CandleCalculationTests
{
    private static DateTimeOffset Ts(int h, int m, int s, int ms = 0)
        => new(2024, 3, 13, h, m, s, ms, TimeSpan.Zero);

    private static SessionCalendar ForexUtc => new(ServerTimeRule.Utc, SymbolGroup.Forex);

    [Fact]
    public void TicksToM1_GoldenVector()
    {
        var ticks = new[]
        {
            new Tick(Ts(12, 0, 5, 0), 1.10000, 1.10010, 0),
            new Tick(Ts(12, 0, 20, 0), 1.10020, 1.10030, 0),
            new Tick(Ts(12, 0, 40, 0), 1.09990, 1.10000, 0),
            new Tick(Ts(12, 0, 59, 900), 1.10005, 1.10015, 0),
            new Tick(Ts(12, 1, 3, 0), 1.10010, 1.10020, 0),
        };

        var m1 = new TickAggregator(Timeframe.M1, ForexUtc);
        var m5 = new TickAggregator(Timeframe.M5, ForexUtc);
        int m1Closed = 0, m5Closed = 0;

        foreach (var t in ticks)
        {
            if (m1.Apply(t, out _)) m1Closed++;
            if (m5.Apply(t, out _)) m5Closed++;
        }

        // exactly one CandleClosed(M1,12:00) and no CandleClosed for M5
        Assert.Equal(1, m1Closed);
        Assert.Equal(0, m5Closed);

        // M1 12:00 closed: O 1.10000 H 1.10020 L 1.09990 C 1.10005 V 4
        Assert.Single(m1.ClosedCandles);
        var closed = m1.ClosedCandles[0];
        Assert.Equal(Ts(12, 0, 0), closed.Timestamp);
        Assert.Equal(1.10000, closed.Open);
        Assert.Equal(1.10020, closed.High);
        Assert.Equal(1.09990, closed.Low);
        Assert.Equal(1.10005, closed.Close);
        Assert.Equal(4, closed.Volume);

        // M1 12:01 forming: O 1.10010 H 1.10010 L 1.10010 C 1.10010 V 1
        Assert.False(m1.Forming.IsEmpty);
        Assert.Equal(Ts(12, 1, 0), m1.Forming.Timestamp);
        Assert.Equal(1.10010, m1.Forming.Open);
        Assert.Equal(1.10010, m1.Forming.High);
        Assert.Equal(1.10010, m1.Forming.Low);
        Assert.Equal(1.10010, m1.Forming.Close);
        Assert.Equal(1, m1.Forming.Volume);

        // M5 12:00 forming: O 1.10000 H 1.10020 L 1.09990 C 1.10010 V 5
        Assert.False(m5.Forming.IsEmpty);
        Assert.Equal(Ts(12, 0, 0), m5.Forming.Timestamp);
        Assert.Equal(1.10000, m5.Forming.Open);
        Assert.Equal(1.10020, m5.Forming.High);
        Assert.Equal(1.09990, m5.Forming.Low);
        Assert.Equal(1.10010, m5.Forming.Close);
        Assert.Equal(5, m5.Forming.Volume);
    }

    [Fact]
    public void FoldM5_GoldenVector()
    {
        var m1 = new List<Candle>
        {
            new(Ts(12, 0, 0), 1.1000, 1.1010, 1.0995, 1.1005, 10),
            new(Ts(12, 1, 0), 1.1005, 1.1020, 1.1000, 1.1015, 12),
            new(Ts(12, 2, 0), 1.1015, 1.1018, 1.0990, 1.0995, 8),
            new(Ts(12, 3, 0), 1.0995, 1.1000, 1.0985, 1.0990, 9),
            new(Ts(12, 4, 0), 1.0990, 1.1005, 1.0988, 1.1002, 11),
        };

        var folded = CandleFold.Fold(m1, Ts(12, 0, 0));
        Assert.Equal(Ts(12, 0, 0), folded.Timestamp);
        Assert.Equal(1.1000, folded.Open);
        Assert.Equal(1.1020, folded.High);
        Assert.Equal(1.0985, folded.Low);
        Assert.Equal(1.1002, folded.Close);
        Assert.Equal(50, folded.Volume);
    }

    [Fact]
    public void Aggregate_M1_To_M5()
    {
        var m1 = new List<Candle>
        {
            new(Ts(12, 0, 0), 1.1000, 1.1010, 1.0995, 1.1005, 10),
            new(Ts(12, 1, 0), 1.1005, 1.1020, 1.1000, 1.1015, 12),
            new(Ts(12, 2, 0), 1.1015, 1.1018, 1.0990, 1.0995, 8),
            new(Ts(12, 3, 0), 1.0995, 1.1000, 1.0985, 1.0990, 9),
            new(Ts(12, 4, 0), 1.0990, 1.1005, 1.0988, 1.1002, 11),
        };

        var m5 = CandleFold.Aggregate(m1, Timeframe.M1, Timeframe.M5, ForexUtc);
        Assert.Single(m5);
        Assert.Equal(1.1000, m5[0].Open);
        Assert.Equal(1.1020, m5[0].High);
        Assert.Equal(1.0985, m5[0].Low);
        Assert.Equal(1.1002, m5[0].Close);
        Assert.Equal(50, m5[0].Volume);
    }
}
