using MyChart.Core.Models.Market;
using MyChart.Core.Scale;
using Xunit;

namespace MyChart.Tests.Scale;

/// <summary>T2.01 VERIFY = GOLDEN_TEST_VECTORS.TimeIndex</summary>
public class TimeIndexMapperTests
{
    private static DateTimeOffset U(int y, int m, int d, int h, int min = 0)
        => new(y, m, d, h, min, 0, TimeSpan.Zero);

    private static TimeIndexMapper CreateH1()
    {
        // bar Opens: index0 2024-03-15T20:00Z ; index1 2024-03-15T21:00Z ; index2 2024-03-17T22:00Z
        var opens = new[]
        {
            U(2024, 3, 15, 20),
            U(2024, 3, 15, 21),
            U(2024, 3, 17, 22),
        };
        var cal = new SessionCalendar(ServerTimeRule.Utc, SymbolGroup.Forex);
        return new TimeIndexMapper(opens, Timeframe.H1, cal);
    }

    [Fact]
    public void IndexOfTime_GoldenVector()
    {
        var map = CreateH1();

        Assert.Equal(0.5, map.IndexOfTime(U(2024, 3, 15, 20, 30)), 9);
        Assert.Equal(1.0, map.IndexOfTime(U(2024, 3, 15, 21, 0)), 9);
        // in the gap after bar 1
        Assert.Equal(2.0, map.IndexOfTime(U(2024, 3, 15, 22, 30)), 9);
        Assert.Equal(2.5, map.IndexOfTime(U(2024, 3, 17, 22, 30)), 9);
        Assert.Equal(4.0, map.IndexOfTime(U(2024, 3, 18, 0, 0)), 9);
    }

    [Fact]
    public void TimeAtIndex_GoldenVector()
    {
        var map = CreateH1();

        Assert.Equal(U(2024, 3, 15, 20, 30), map.TimeAtIndex(0.5));
        Assert.Equal(U(2024, 3, 17, 22, 0), map.TimeAtIndex(2.0));
        Assert.Equal(U(2024, 3, 18, 0, 0), map.TimeAtIndex(4.0));
    }

    [Fact]
    public void SnapIndex_GoldenVector()
    {
        var map = CreateH1();
        Assert.Equal(2, map.SnapIndex(1.5));
        Assert.Equal(1, map.SnapIndex(1.49));
    }

    [Fact]
    public void IndexOfTime_EmptySeries_ReturnsZero()
    {
        var cal = new SessionCalendar(ServerTimeRule.Utc, SymbolGroup.Forex);
        var map = new TimeIndexMapper(Array.Empty<DateTimeOffset>(), Timeframe.H1, cal);
        Assert.Equal(0, map.IndexOfTime(U(2024, 1, 1, 0)));
    }

    [Fact]
    public void RoundTrip_InsideBar()
    {
        var map = CreateH1();
        var t = U(2024, 3, 15, 20, 30);
        var u = map.IndexOfTime(t);
        Assert.Equal(t, map.TimeAtIndex(u));
    }
}
