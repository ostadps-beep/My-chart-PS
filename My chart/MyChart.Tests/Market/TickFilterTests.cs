using MyChart.Core.Candles;
using MyChart.Core.Models.Market;
using Xunit;

namespace MyChart.Tests.Market;

/// <summary>
/// T1.05 VERIFY = GOLDEN_TEST_VECTORS.TickFilter
/// </summary>
public class TickFilterTests
{
    private static DateTimeOffset Ts(int h, int m, int s, int ms = 0)
        => new(2024, 3, 13, h, m, s, ms, TimeSpan.Zero);

    private static Tick T(DateTimeOffset ts, double bid, double ask, double vol = 0)
        => new(ts, bid, ask, vol);

    [Fact]
    public void TickFilter_GoldenVector()
    {
        var filter = new TickFilter();
        var now = Ts(12, 5, 0);

        // accepted: 12:00:00.000 and 12:00:00.500
        Assert.True(filter.TryAccept(T(Ts(12, 0, 0, 0), 1.10000, 1.10010), now, out _));
        Assert.True(filter.TryAccept(T(Ts(12, 0, 0, 500), 1.10005, 1.10015), now, out _));

        // rejected out of order: 12:00:00.400
        Assert.False(filter.TryAccept(T(Ts(12, 0, 0, 400), 1.10002, 1.10012), now, out _));

        // rejected crossed: Ask < Bid
        Assert.False(filter.TryAccept(T(Ts(12, 0, 1, 0), 1.10010, 1.10005), now, out _));

        // build 10 accepted near 1.10000 for spike threshold
        // already have 2; add 8 more
        for (int i = 0; i < 8; i++)
        {
            Assert.True(filter.TryAccept(
                T(Ts(12, 0, 2 + i, 0), 1.10000 + i * 0.00001, 1.10010 + i * 0.00001), now, out _));
        }

        // spike: 1.20000 rejected; 1.20005 rejected; 1.20002 accepted as new level
        Assert.False(filter.TryAccept(T(Ts(12, 0, 20, 0), 1.20000, 1.20010), now, out _));
        Assert.False(filter.TryAccept(T(Ts(12, 0, 21, 0), 1.20005, 1.20015), now, out _));
        Assert.True(filter.TryAccept(T(Ts(12, 0, 22, 0), 1.20002, 1.20012), now, out var accepted));
        Assert.Equal(1.20002, accepted.Bid);
    }

    [Fact]
    public void TickBuffer_OverflowDropsOldest()
    {
        var buf = new TickBuffer(capacity: 3);
        var now = Ts(12, 0, 0);

        buf.Enqueue(T(Ts(12, 0, 0), 1.0, 1.1));
        buf.Enqueue(T(Ts(12, 0, 1), 1.0, 1.1));
        buf.Enqueue(T(Ts(12, 0, 2), 1.0, 1.1));
        Assert.Equal(3, buf.Count);
        Assert.Equal(0, buf.OverflowCount);

        buf.Enqueue(T(Ts(12, 0, 3), 1.0, 1.1)); // drops oldest
        Assert.Equal(3, buf.Count);
        Assert.Equal(1, buf.OverflowCount);

        var list = new List<Tick>();
        buf.DrainToList(list, max: 5000);
        Assert.Equal(3, list.Count);
        Assert.Equal(Ts(12, 0, 1), list[0].Timestamp); // oldest was 12:00:00, dropped
        Assert.Equal(Ts(12, 0, 3), list[2].Timestamp);
    }

    [Fact]
    public void TickBuffer_DrainMax5000()
    {
        var buf = new TickBuffer(capacity: 10);
        for (int i = 0; i < 8; i++)
            buf.Enqueue(T(Ts(12, 0, i), 1.0, 1.1));

        var list = new List<Tick>();
        int n = buf.DrainToList(list, max: 5);
        Assert.Equal(5, n);
        Assert.Equal(3, buf.Count);
    }
}
