using MyChart.Core.Models.Market;
using MyChart.Data.Aggregation;
using Xunit;

namespace MyChart.Tests.Market;

/// <summary>T1.11 — forming is last element when HasForming.</summary>
public class AggregationEngineTests
{
    [Fact]
    public void OnTick_FormingIsLast_HasForming()
    {
        var engine = new AggregationEngine();
        var t0 = new DateTimeOffset(2024, 3, 13, 12, 0, 5, TimeSpan.Zero);
        engine.OnTick("EURUSD", new Tick(t0, 1.10000, 1.10010, 0));

        var series = engine.GetSeries("EURUSD", Timeframe.M1);
        Assert.True(series.HasForming);
        Assert.Equal(1, series.Count);
        Assert.Equal(1.10000, series[0].Open);
        Assert.Equal(t0.Date.AddHours(12), series.OpenTime(0).Date.AddHours(series.OpenTime(0).Hour)); // bucket open
    }

    [Fact]
    public void SetBaseHistory_ThenGetSeries()
    {
        var engine = new AggregationEngine();
        var candles = new[]
        {
            new Candle(new DateTimeOffset(2024, 3, 13, 12, 0, 0, TimeSpan.Zero), 1.1, 1.1, 1.1, 1.1, 1),
            new Candle(new DateTimeOffset(2024, 3, 13, 12, 1, 0, TimeSpan.Zero), 1.2, 1.2, 1.2, 1.2, 1),
        };
        engine.SetBaseHistory("EURUSD", candles);
        var s = engine.GetSeries("EURUSD", Timeframe.M1);
        Assert.Equal(2, s.Count);
        Assert.False(s.HasForming);
    }
}
