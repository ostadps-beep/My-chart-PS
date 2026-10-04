using MyChart.Core.Candles;
using MyChart.Core.Models.Market;
using Xunit;

namespace MyChart.Tests.Market;

/// <summary>
/// T1.02 VERIFY = GOLDEN_TEST_VECTORS.BucketFloor
/// </summary>
public class TimeBucketsTests
{
    private static DateTimeOffset Utc(int y, int m, int d, int h, int min, int s = 0)
        => new(y, m, d, h, min, s, TimeSpan.Zero);

    private static SessionCalendar ForexUtc => new(ServerTimeRule.Utc, SymbolGroup.Forex);
    private static SessionCalendar StocksUtc => new(ServerTimeRule.Utc, SymbolGroup.Stocks);
    private static SessionCalendar ForexFixed2 => new(new ServerTimeRule.Fixed(TimeSpan.FromHours(2)), SymbolGroup.Forex);
    private static SessionCalendar ForexEet => new(new ServerTimeRule.EetUsDst(), SymbolGroup.Forex);

    [Fact]
    public void BucketFloor_Wednesday_Fixed0_Forex()
    {
        // t = 2024-03-13T14:37:42Z (Wednesday)
        var t = Utc(2024, 3, 13, 14, 37, 42);
        var cal = ForexUtc;

        Assert.Equal(Utc(2024, 3, 13, 14, 37), TimeBuckets.Floor(t, Timeframe.M1, cal));
        Assert.Equal(Utc(2024, 3, 13, 14, 35), TimeBuckets.Floor(t, Timeframe.M5, cal));
        Assert.Equal(Utc(2024, 3, 13, 14, 30), TimeBuckets.Floor(t, Timeframe.M15, cal));
        Assert.Equal(Utc(2024, 3, 13, 14, 30), TimeBuckets.Floor(t, Timeframe.M30, cal));
        Assert.Equal(Utc(2024, 3, 13, 14, 0), TimeBuckets.Floor(t, Timeframe.H1, cal));
        Assert.Equal(Utc(2024, 3, 13, 12, 0), TimeBuckets.Floor(t, Timeframe.H4, cal));
        Assert.Equal(Utc(2024, 3, 13, 0, 0), TimeBuckets.Floor(t, Timeframe.D1, cal));
        Assert.Equal(Utc(2024, 3, 11, 0, 0), TimeBuckets.Floor(t, Timeframe.W1, cal)); // Monday
        Assert.Equal(Utc(2024, 3, 1, 0, 0), TimeBuckets.Floor(t, Timeframe.MN1, cal));
    }

    [Fact]
    public void BucketFloor_FixedPlus2_D1_H4()
    {
        // Fixed(+02:00), t = 2024-03-13T22:30:00Z -> D1 2024-03-13T22:00:00Z ; H4 2024-03-13T22:00:00Z
        var t = Utc(2024, 3, 13, 22, 30);
        var cal = ForexFixed2;

        Assert.Equal(Utc(2024, 3, 13, 22, 0), TimeBuckets.Floor(t, Timeframe.D1, cal));
        Assert.Equal(Utc(2024, 3, 13, 22, 0), TimeBuckets.Floor(t, Timeframe.H4, cal));
    }

    [Fact]
    public void BucketFloor_Sunday_W1_ForexVsStocks()
    {
        // Fixed(+00:00), t = 2024-03-17T22:00:00Z (Sunday)
        // Forex W1 2024-03-18T00:00:00Z ; Stocks W1 2024-03-11T00:00:00Z ; D1 2024-03-17T00:00:00Z
        var t = Utc(2024, 3, 17, 22, 0);

        Assert.Equal(Utc(2024, 3, 18, 0, 0), TimeBuckets.Floor(t, Timeframe.W1, ForexUtc));
        Assert.Equal(Utc(2024, 3, 11, 0, 0), TimeBuckets.Floor(t, Timeframe.W1, StocksUtc));
        Assert.Equal(Utc(2024, 3, 17, 0, 0), TimeBuckets.Floor(t, Timeframe.D1, ForexUtc));
    }

    [Fact]
    public void BucketFloor_EetUsDst_Sunday()
    {
        // EET_US_DST, t = 2024-03-17T22:00:00Z -> D1 2024-03-17T21:00:00Z ; W1 2024-03-17T21:00:00Z
        var t = Utc(2024, 3, 17, 22, 0);
        var cal = ForexEet;

        Assert.Equal(Utc(2024, 3, 17, 21, 0), TimeBuckets.Floor(t, Timeframe.D1, cal));
        Assert.Equal(Utc(2024, 3, 17, 21, 0), TimeBuckets.Floor(t, Timeframe.W1, cal));
    }

    [Fact]
    public void BucketFloor_EetUsDst_Winter()
    {
        // EET_US_DST, t = 2024-01-10T21:30:00Z -> D1 2024-01-09T22:00:00Z (winter, UTC+2)
        var t = Utc(2024, 1, 10, 21, 30);
        Assert.Equal(Utc(2024, 1, 9, 22, 0), TimeBuckets.Floor(t, Timeframe.D1, ForexEet));
    }

    [Fact]
    public void BucketFloor_EetUsDst_Summer()
    {
        // EET_US_DST, t = 2024-07-10T21:30:00Z -> D1 2024-07-10T21:00:00Z (summer, UTC+3)
        var t = Utc(2024, 7, 10, 21, 30);
        Assert.Equal(Utc(2024, 7, 10, 21, 0), TimeBuckets.Floor(t, Timeframe.D1, ForexEet));
    }

    [Fact]
    public void BucketFloor_LeapDay_MN1()
    {
        // Fixed(+00:00), t = 2024-02-29T23:59:00Z -> MN1 2024-02-01T00:00:00Z
        var t = Utc(2024, 2, 29, 23, 59);
        Assert.Equal(Utc(2024, 2, 1, 0, 0), TimeBuckets.Floor(t, Timeframe.MN1, ForexUtc));
    }

    [Fact]
    public void Next_Prev_Span_M1()
    {
        var start = Utc(2024, 3, 13, 14, 37);
        var cal = ForexUtc;
        var next = TimeBuckets.Next(start, Timeframe.M1, cal);
        Assert.Equal(Utc(2024, 3, 13, 14, 38), next);
        Assert.Equal(start, TimeBuckets.Prev(next, Timeframe.M1, cal));
        Assert.Equal(TimeSpan.FromMinutes(1), TimeBuckets.Span(start, Timeframe.M1, cal));
    }

    [Fact]
    public void Next_Prev_Span_W1()
    {
        var start = Utc(2024, 3, 11, 0, 0); // Monday
        var cal = ForexUtc;
        var next = TimeBuckets.Next(start, Timeframe.W1, cal);
        Assert.Equal(Utc(2024, 3, 18, 0, 0), next);
        Assert.Equal(start, TimeBuckets.Prev(next, Timeframe.W1, cal));
        Assert.Equal(TimeSpan.FromDays(7), TimeBuckets.Span(start, Timeframe.W1, cal));
    }

    [Fact]
    public void Next_Prev_Span_MN1()
    {
        var start = Utc(2024, 2, 1, 0, 0);
        var cal = ForexUtc;
        var next = TimeBuckets.Next(start, Timeframe.MN1, cal);
        Assert.Equal(Utc(2024, 3, 1, 0, 0), next);
        Assert.Equal(start, TimeBuckets.Prev(next, Timeframe.MN1, cal));
    }
}
