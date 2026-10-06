using MyChart.Core.Candles;
using MyChart.Core.Models.Market;
using MyChart.Core.Services;
using MyChart.Data.Aggregation;
using MyChart.Data.Replay;
using Xunit;

namespace MyChart.Tests.Replay;

/// <summary>
/// T5.05 VERIFY — replaying Fixtures/ticks_sample.csv gives the same candles as live processing.
/// </summary>
public class ReplayEngineTests
{
    private static DateTimeOffset Ts(int h, int m, int s, int ms = 0)
        => new(2024, 3, 13, h, m, s, ms, TimeSpan.Zero);

    private static SessionCalendar ForexUtc => new(ServerTimeRule.Utc, SymbolGroup.Forex);

    private static Tick[] GoldenTicks() =>
    [
        new Tick(Ts(12, 0, 5, 0), 1.10000, 1.10010, 0),
        new Tick(Ts(12, 0, 20, 0), 1.10020, 1.10030, 0),
        new Tick(Ts(12, 0, 40, 0), 1.09990, 1.10000, 0),
        new Tick(Ts(12, 0, 59, 900), 1.10005, 1.10015, 0),
        new Tick(Ts(12, 1, 3, 0), 1.10010, 1.10020, 0),
    ];

    [Fact]
    public void Backtest_Matches_Live_TickAggregator_Golden()
    {
        var live = new TickAggregator(Timeframe.M1, ForexUtc);
        foreach (var t in GoldenTicks())
            live.Apply(t, out _);

        var agg = new AggregationEngine(ForexUtc);
        var clock = new ReplayClock(Ts(12, 0, 0));
        var engine = new ReplayEngine("EURUSD", agg, clock);
        engine.LoadTicks(GoldenTicks());
        var applied = engine.RunBacktestToEnd();

        Assert.Equal(5, applied);
        Assert.True(engine.IsFinished);

        var series = agg.GetSeries("EURUSD", Timeframe.M1);
        Assert.Equal(live.ClosedCandles.Count + (live.Forming.IsEmpty ? 0 : 1), series.Count);
        Assert.True(series.HasForming);

        var closed = series[0];
        Assert.Equal(Ts(12, 0, 0), closed.Timestamp);
        Assert.Equal(1.10000, closed.Open, 5);
        Assert.Equal(1.10020, closed.High, 5);
        Assert.Equal(1.09990, closed.Low, 5);
        Assert.Equal(1.10005, closed.Close, 5);
        Assert.Equal(4, closed.Volume, 5);

        var forming = series[1];
        Assert.Equal(Ts(12, 1, 0), forming.Timestamp);
        Assert.Equal(1.10010, forming.Open, 5);
    }

    [Fact]
    public void CsvFixture_Matches_Live()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "ticks_sample.csv");
        Assert.True(File.Exists(path), $"Missing fixture: {path}");

        var ticks = TickCsvLoader.Load(path);

        var live = new TickAggregator(Timeframe.M1, ForexUtc);
        foreach (var t in ticks)
            live.Apply(t, out _);

        var agg = new AggregationEngine(ForexUtc);
        var clock = new ReplayClock(ticks[0].Timestamp);
        var engine = new ReplayEngine("EURUSD", agg, clock);
        engine.LoadTicks(ticks);
        engine.RunBacktestToEnd();

        var series = agg.GetSeries("EURUSD", Timeframe.M1);
        Assert.Equal(live.ClosedCandles.Count + (live.Forming.IsEmpty ? 0 : 1), series.Count);
        Assert.Equal(live.ClosedCandles[0].Open, series[0].Open, 5);
        Assert.Equal(live.ClosedCandles[0].Close, series[0].Close, 5);
        Assert.Equal(live.ClosedCandles[0].High, series[0].High, 5);
        Assert.Equal(live.ClosedCandles[0].Low, series[0].Low, 5);
    }

    [Fact]
    public void StepForward_Training_OneTickAtATime()
    {
        var agg = new AggregationEngine(ForexUtc);
        var clock = new ReplayClock(Ts(12, 0, 0));
        var engine = new ReplayEngine("EURUSD", agg, clock);
        engine.LoadTicks(GoldenTicks());
        engine.SetMode(ReplayMode.Training);

        Assert.True(engine.StepForward());
        Assert.Equal(1, engine.CursorIndex);
        Assert.True(engine.StepForward());
        Assert.Equal(2, engine.CursorIndex);

        while (engine.StepForward()) { }
        Assert.True(engine.IsFinished);
        Assert.Equal(5, engine.CursorIndex);
    }

    [Fact]
    public void PumpDueTicks_Respects_Clock()
    {
        var agg = new AggregationEngine(ForexUtc);
        var clock = new ReplayClock(Ts(12, 0, 0));
        var engine = new ReplayEngine("EURUSD", agg, clock);
        engine.LoadTicks(GoldenTicks());
        engine.SetMode(ReplayMode.MarketReplay);
        engine.SetSpeed(1);

        clock.Set(Ts(12, 0, 30));
        var n = engine.PumpDueTicks();
        Assert.Equal(2, n);

        clock.Set(Ts(12, 1, 10));
        n = engine.PumpDueTicks();
        Assert.Equal(3, n);
        Assert.True(engine.IsFinished);
    }

    [Fact]
    public void SetSpeed_Rejects_Invalid()
    {
        var engine = new ReplayEngine("EURUSD", new AggregationEngine(ForexUtc), new ReplayClock(Ts(12, 0, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => engine.SetSpeed(3));
    }

    [Fact]
    public void VisibleBars_Training_Hides_After_Cursor()
    {
        var bars = new[]
        {
            new Candle(Ts(12, 0, 0), 1, 1, 1, 1, 1),
            new Candle(Ts(12, 1, 0), 1, 1, 1, 1, 1),
            new Candle(Ts(12, 2, 0), 1, 1, 1, 1, 1),
        };
        var visible = ReplayEngine.VisibleBars(bars, Ts(12, 1, 0));
        Assert.Equal(2, visible.Count);
        Assert.Equal(Ts(12, 1, 0), visible[^1].Timestamp);
    }

    [Fact]
    public void AllowedSpeeds_Are_1_2_5_10_50()
    {
        Assert.Equal(new double[] { 1, 2, 5, 10, 50 }, ReplayEngine.AllowedSpeeds);
    }
}
