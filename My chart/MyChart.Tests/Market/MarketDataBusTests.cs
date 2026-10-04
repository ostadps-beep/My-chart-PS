using MyChart.Core.Candles;
using MyChart.Core.Models.Market;
using Xunit;

namespace MyChart.Tests.Market;

/// <summary>T1.07 VERIFY — order of delivery; coalescing; exception isolation.</summary>
public class MarketDataBusTests
{
    [Fact]
    public void Publish_OrderOfDelivery()
    {
        var bus = new MarketDataBus();
        var order = new List<string>();

        bus.Subscribe<TickReceived>(_ => order.Add("tick"));
        bus.Subscribe<CandleClosed>(_ => order.Add("closed"));

        var tick = new Tick(DateTimeOffset.UtcNow, 1.1, 1.2, 0);
        var candle = new Candle(DateTimeOffset.UtcNow, 1, 1, 1, 1, 1);

        bus.Publish(new TickReceived("EURUSD", tick));
        bus.Publish(new CandleClosed("EURUSD", Timeframe.M1, candle));

        Assert.Equal(new[] { "tick", "closed" }, order);
    }

    [Fact]
    public void CandleClosed_NeverDropped()
    {
        var bus = new MarketDataBus();
        int closed = 0;
        bus.Subscribe<CandleClosed>(_ => closed++);

        var c = new Candle(DateTimeOffset.UtcNow, 1, 1, 1, 1, 1);
        bus.Publish(new CandleClosed("EURUSD", Timeframe.M1, c));
        bus.Publish(new CandleClosed("EURUSD", Timeframe.M1, c));

        Assert.Equal(2, closed);
    }

    [Fact]
    public void CandleUpdated_CoalescedPerSymbolTimeframe()
    {
        var bus = new MarketDataBus();
        var received = new List<CandleUpdated>();
        bus.Subscribe<CandleUpdated>(e => received.Add(e));

        var c1 = new Candle(DateTimeOffset.UtcNow, 1, 1, 1, 1, 1);
        var c2 = new Candle(DateTimeOffset.UtcNow, 2, 2, 2, 2, 2);

        // Multiple updates in same frame — only last should flush
        bus.Publish(new CandleUpdated("EURUSD", Timeframe.M1, c1));
        bus.Publish(new CandleUpdated("EURUSD", Timeframe.M1, c2));
        bus.FlushFrame();

        Assert.Single(received);
        Assert.Equal(2, received[0].Forming.Close);
    }

    [Fact]
    public void ExceptionIsolation_DoesNotStopOtherSubscribers()
    {
        var bus = new MarketDataBus();
        int good = 0;

        bus.Subscribe<TickReceived>(_ => throw new InvalidOperationException("boom"));
        bus.Subscribe<TickReceived>(_ => good++);

        var tick = new Tick(DateTimeOffset.UtcNow, 1.1, 1.2, 0);
        bus.Publish(new TickReceived("EURUSD", tick));

        Assert.Equal(1, good);
    }
}
