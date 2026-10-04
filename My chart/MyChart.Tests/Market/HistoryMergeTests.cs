using MyChart.Core.Candles;
using MyChart.Core.Models.Market;
using Xunit;

namespace MyChart.Tests.Market;

/// <summary>T1.08 VERIFY = GOLDEN_TEST_VECTORS.Merge</summary>
public class HistoryMergeTests
{
    private static DateTimeOffset T(int h, int m = 0)
        => new(2024, 3, 13, h, m, 0, TimeSpan.Zero);

    [Fact]
    public void Merge_GoldenVector_IncomingWins()
    {
        // existing M1: 12:00 C 1.1000 ; 12:01 C 1.1001 ; 12:02 C 1.1002
        var existing = new List<Candle>
        {
            new(T(12, 0), 1.1000, 1.1000, 1.1000, 1.1000, 1),
            new(T(12, 1), 1.1001, 1.1001, 1.1001, 1.1001, 1),
            new(T(12, 2), 1.1002, 1.1002, 1.1002, 1.1002, 1),
        };

        // incoming M1: 12:02 C 1.1009 ; 12:03 C 1.1003
        var incoming = new List<Candle>
        {
            new(T(12, 2), 1.1009, 1.1009, 1.1009, 1.1009, 1),
            new(T(12, 3), 1.1003, 1.1003, 1.1003, 1.1003, 1),
        };

        var result = HistoryMerge.Merge(existing, incoming);

        // result: 12:00 C 1.1000 ; 12:01 C 1.1001 ; 12:02 C 1.1009 ; 12:03 C 1.1003
        Assert.Equal(4, result.Count);
        Assert.Equal(1.1000, result[0].Close);
        Assert.Equal(1.1001, result[1].Close);
        Assert.Equal(1.1009, result[2].Close); // incoming wins
        Assert.Equal(1.1003, result[3].Close);

        // strictly ascending
        for (int i = 1; i < result.Count; i++)
            Assert.True(result[i].Timestamp > result[i - 1].Timestamp);
    }

    [Fact]
    public void UpdateFrom_GoesBackTwoBars()
    {
        var cal = new SessionCalendar(ServerTimeRule.Utc, SymbolGroup.Forex);
        var last = T(12, 10);
        var from = HistoryMerge.UpdateFrom(last, Timeframe.M1, cal);
        Assert.Equal(T(12, 8), from);
    }
}
