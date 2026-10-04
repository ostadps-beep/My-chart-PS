using MyChart.Core.Candles;
using MyChart.Core.Models.Market;
using Xunit;

namespace MyChart.Tests.Market;

/// <summary>
/// T1.03 VERIFY = GOLDEN_TEST_VECTORS.Validation
/// </summary>
public class DataValidatorTests
{
    private static DateTimeOffset T(int hour, int minute = 0)
        => new(2024, 3, 13, hour, minute, 0, TimeSpan.Zero);

    private static SessionCalendar ForexUtc => new(ServerTimeRule.Utc, SymbolGroup.Forex);

    [Fact]
    public void Validation_GoldenVector_M1_Digits5_Forex()
    {
        // input rows in order
        var input = new List<Candle>
        {
            new(T(12, 0), 1.10000, 1.10010, 1.09990, 1.10005, 5),   // a
            new(T(12, 1), 1.10005, 1.10000, 1.09990, 1.10020, 3),   // b High below Close
            new(T(12, 2), -1.0, 1.1, 1.0, 1.1, 1),                  // c corrupted
            new(T(12, 3), 1.10000, 1.10000, 1.10000, 1.10000, 1),   // d1
            new(T(12, 3), 1.10030, 1.10040, 1.10020, 1.10035, 2),   // d2 duplicate
            new(T(12, 7), 1.10035, 1.10050, 1.10030, 1.10040, 4),   // e
        };

        var now = new DateTimeOffset(2024, 3, 13, 18, 0, 0, TimeSpan.Zero);
        var (candles, report) = DataValidator.Validate(input, Timeframe.M1, 5, ForexUtc, now);

        // result candles: a, b with High 1.10020, d2, e (4 candles)
        Assert.Equal(4, candles.Count);

        Assert.Equal(T(12, 0), candles[0].Timestamp);
        Assert.Equal(1.10000, candles[0].Open);
        Assert.Equal(1.10010, candles[0].High);
        Assert.Equal(1.09990, candles[0].Low);
        Assert.Equal(1.10005, candles[0].Close);
        Assert.Equal(5, candles[0].Volume);

        Assert.Equal(T(12, 1), candles[1].Timestamp);
        Assert.Equal(1.10005, candles[1].Open);
        Assert.Equal(1.10020, candles[1].High); // repaired
        Assert.Equal(1.09990, candles[1].Low);
        Assert.Equal(1.10020, candles[1].Close);
        Assert.Equal(3, candles[1].Volume);

        Assert.Equal(T(12, 3), candles[2].Timestamp);
        Assert.Equal(1.10030, candles[2].Open); // d2 wins
        Assert.Equal(1.10040, candles[2].High);
        Assert.Equal(1.10020, candles[2].Low);
        Assert.Equal(1.10035, candles[2].Close);
        Assert.Equal(2, candles[2].Volume);

        Assert.Equal(T(12, 7), candles[3].Timestamp);
        Assert.Equal(1.10035, candles[3].Open);

        // report: Accepted 4, Repaired 1, Rejected 1, Corrupted 1, Duplicates 1
        Assert.Equal(4, report.Accepted);
        Assert.Equal(1, report.Repaired);
        Assert.Equal(1, report.Rejected);
        Assert.Equal(1, report.Corrupted);
        Assert.Equal(1, report.Duplicates);

        // Spec gap: From 12:03 To 12:07 MissingBars 3 Kind Missing
        // Also a gap 12:01→12:03 (MissingBars 1) because corrupted 12:02 was removed — correct behaviour
        Assert.Equal(2, report.Gaps.Count);

        var gap1203 = report.Gaps.Single(g => g.From == T(12, 3));
        Assert.Equal(T(12, 7), gap1203.To);
        Assert.Equal(3, gap1203.MissingBars);
        Assert.Equal(GapKind.Missing, gap1203.Kind);

        var gap1201 = report.Gaps.Single(g => g.From == T(12, 1));
        Assert.Equal(T(12, 3), gap1201.To);
        Assert.Equal(1, gap1201.MissingBars);
        Assert.Equal(GapKind.Missing, gap1201.Kind);
    }

    [Fact]
    public void GapKind_Weekend_Forex_IsExpected()
    {
        // neighbours Friday 21:59Z and Sunday 22:00Z (Forex, Fixed 0) -> Kind Expected
        var from = new DateTimeOffset(2024, 3, 15, 21, 59, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2024, 3, 17, 22, 0, 0, TimeSpan.Zero);
        var kind = DataValidator.ClassifyGap(from, to, ForexUtc);
        Assert.Equal(GapKind.Expected, kind);
    }

    [Fact]
    public void RoundPrice_AwayFromZero()
    {
        // Use values that are exact in binary or compare with tolerance after known midpoint rule
        Assert.Equal(1.10001, DataValidator.RoundPrice(1.1000051, 5), 8);
        Assert.Equal(1.10000, DataValidator.RoundPrice(1.1000041, 5), 8);
        Assert.Equal(1.25, DataValidator.RoundPrice(1.245, 2), 8); // AwayFromZero midpoint
        Assert.Equal(1.24, DataValidator.RoundPrice(1.244, 2), 8);
    }
}
