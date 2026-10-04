using MyChart.Core.Analysis;
using MyChart.Core.Models.Chart;
using MyChart.Core.Models.Market;
using MyChart.Core.Models.Viewport;
using Xunit;

namespace MyChart.Tests.Analysis;

/// <summary>
/// T3.02 HudData — each field against a fake session, the adaptive presentation, MarketStatus rules,
/// FPS meter and the refresh rule (1000 mouse moves inside one bar = 0 refreshes).
/// Fake session: 100 M15 bars from 2024-03-13T12:00Z (Volume = 100 + index), visible range from 5 to 99.
/// </summary>
public class HudDataTests
{
    private static readonly SymbolInfo Eurusd = new("EURUSD", "EURUSD", SymbolGroup.Forex, 5);
    private static readonly SymbolInfo Usdjpy = new("USDJPY", "USDJPY", SymbolGroup.Forex, 3);
    private static readonly DateTimeOffset Start = new(2024, 3, 13, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Now = Start.AddHours(30);
    private static readonly RenderWindowRange DefaultRange = new(5, 99, 4, 99, 95);

    private static List<Candle> MakeBars(int n)
    {
        var bars = new List<Candle>();
        for (int i = 0; i < n; i++)
            bars.Add(new Candle(Start.AddMinutes(15 * i), 1.1000, 1.1010, 1.0990, 1.1005, 100 + i));
        return bars;
    }

    private static Tick TickAt(DateTimeOffset ts, double bid, double ask) => new(ts, bid, ask, 1);

    private static HudInput MakeInput(
        double plotWidth = 1000,
        CrosshairState? crosshair = null,
        Tick? tick = null,
        bool replay = false,
        bool connected = true,
        HudField advanced = HudField.None,
        double barSpacing = 8,
        TimeZoneInfo? zone = null,
        IReadOnlyList<Candle>? bars = null,
        RenderWindowRange? range = null)
    {
        return new HudInput(
            Eurusd,
            Timeframe.M15,
            bars ?? MakeBars(100),
            range ?? DefaultRange,
            barSpacing,
            plotWidth,
            crosshair,
            tick,
            Now,
            replay,
            connected,
            "CSV",
            HudCacheStatus.Memory,
            60,
            advanced,
            zone);
    }

    // ---------- ADAPTIVE ----------

    [Fact]
    public void Presentation_ByPlotWidth()
    {
        Assert.Equal(HudPresentation.Full, HudDataProvider.PresentationFor(1400));
        Assert.Equal(HudPresentation.Full, HudDataProvider.PresentationFor(900));
        Assert.Equal(HudPresentation.Compact, HudDataProvider.PresentationFor(899.9));
        Assert.Equal(HudPresentation.Compact, HudDataProvider.PresentationFor(500));
        Assert.Equal(HudPresentation.Minimal, HudDataProvider.PresentationFor(499.9));
        Assert.Equal(HudPresentation.Minimal, HudDataProvider.PresentationFor(0));
    }

    [Fact]
    public void VisibleFields_PerPresentation()
    {
        var all = HudField.Advanced;

        // Full = INFORMATION + enabled ADVANCED_OPTIONAL
        Assert.Equal(HudField.Information, HudDataProvider.VisibleFields(HudPresentation.Full, HudField.None));
        Assert.Equal(
            HudField.Information | HudField.Fps | HudField.ZoomLevel,
            HudDataProvider.VisibleFields(HudPresentation.Full, HudField.Fps | HudField.ZoomLevel));
        Assert.Equal(
            HudField.Information | HudField.Advanced,
            HudDataProvider.VisibleFields(HudPresentation.Full, all));

        // a non-advanced bit in the enabled set adds nothing
        Assert.Equal(HudField.Information, HudDataProvider.VisibleFields(HudPresentation.Full, HudField.Symbol));

        // Compact and Minimal never show advanced fields
        Assert.Equal(
            HudField.Symbol | HudField.Timeframe | HudField.OhlcUnderMouse | HudField.Spread,
            HudDataProvider.VisibleFields(HudPresentation.Compact, all));
        Assert.Equal(
            HudField.Symbol | HudField.Timeframe | HudField.Spread,
            HudDataProvider.VisibleFields(HudPresentation.Minimal, all));
    }

    // ---------- OHLCUnderMouse ----------

    [Fact]
    public void OhlcIndex_InsidePlot_UsesSnapIndex()
    {
        var inside = new CrosshairState { IsInsidePlot = true, SnapIndex = 50 };
        Assert.Equal(50, HudDataProvider.OhlcIndex(inside, 100));

        var first = new CrosshairState { IsInsidePlot = true, SnapIndex = 0 };
        Assert.Equal(0, HudDataProvider.OhlcIndex(first, 100));

        var last = new CrosshairState { IsInsidePlot = true, SnapIndex = 99 };
        Assert.Equal(99, HudDataProvider.OhlcIndex(last, 100));
    }

    [Fact]
    public void OhlcIndex_OutsidePlotOrFutureOrNone_IsLatest()
    {
        var outside = new CrosshairState { IsInsidePlot = false, SnapIndex = 50 };
        Assert.Equal(99, HudDataProvider.OhlcIndex(outside, 100));

        var future = new CrosshairState { IsInsidePlot = true, SnapIndex = 103 };
        Assert.Equal(99, HudDataProvider.OhlcIndex(future, 100));

        var before = new CrosshairState { IsInsidePlot = true, SnapIndex = -1 };
        Assert.Equal(99, HudDataProvider.OhlcIndex(before, 100));

        Assert.Equal(99, HudDataProvider.OhlcIndex(null, 100));
        Assert.Equal(-1, HudDataProvider.OhlcIndex(null, 0));
    }

    // ---------- Spread ----------

    [Fact]
    public void Spread_IsIntegerPoints()
    {
        var eur = HudDataProvider.SpreadPoints(TickAt(Now, 1.10000, 1.10010), Eurusd);
        Assert.True(eur.HasValue);
        Assert.Equal(10, eur.GetValueOrDefault());

        var jpy = HudDataProvider.SpreadPoints(TickAt(Now, 150.000, 150.012), Usdjpy);
        Assert.True(jpy.HasValue);
        Assert.Equal(12, jpy.GetValueOrDefault());
    }

    [Fact]
    public void Spread_NoTick_IsDash()
    {
        var none = HudDataProvider.SpreadPoints(null, Eurusd);
        Assert.False(none.HasValue);
        Assert.Equal("-", HudDataProvider.FormatSpread(none));
        Assert.Equal("10", HudDataProvider.FormatSpread(10));
    }

    // ---------- MarketStatus ----------

    [Fact]
    public void MarketStatus_Rules()
    {
        var fresh = TickAt(Now.AddSeconds(-5), 1.1, 1.1001);

        // Replay wins over everything
        Assert.Equal(HudMarketStatus.Replay, HudDataProvider.MarketStatusOf(true, false, fresh, Now));

        // Offline when the provider is disconnected
        Assert.Equal(HudMarketStatus.Offline, HudDataProvider.MarketStatusOf(false, false, fresh, Now));

        // Live when the last tick is at most 30 s old
        Assert.Equal(HudMarketStatus.Live, HudDataProvider.MarketStatusOf(false, true, fresh, Now));
        Assert.Equal(
            HudMarketStatus.Live,
            HudDataProvider.MarketStatusOf(false, true, TickAt(Now.AddSeconds(-30), 1.1, 1.1001), Now));

        // Stale when older, or when no tick arrived yet
        Assert.Equal(
            HudMarketStatus.Stale,
            HudDataProvider.MarketStatusOf(false, true, TickAt(Now.AddMilliseconds(-30001), 1.1, 1.1001), Now));
        Assert.Equal(HudMarketStatus.Stale, HudDataProvider.MarketStatusOf(false, true, null, Now));

        // a tick time ahead of ServerNow still counts as Live
        Assert.Equal(
            HudMarketStatus.Live,
            HudDataProvider.MarketStatusOf(false, true, TickAt(Now.AddSeconds(10), 1.1, 1.1001), Now));
    }

    // ---------- ZoomLevel ----------

    [Fact]
    public void ZoomLevel_SixDipIs100Percent()
    {
        Assert.Equal(100, HudDataProvider.ZoomLevelPercent(6));
        Assert.Equal(133, HudDataProvider.ZoomLevelPercent(8));
        Assert.Equal(50, HudDataProvider.ZoomLevelPercent(3));
        Assert.Equal(200, HudDataProvider.ZoomLevelPercent(12));
        Assert.Equal(8, HudDataProvider.ZoomLevelPercent(0.5));
        Assert.Equal(833, HudDataProvider.ZoomLevelPercent(50));
    }

    // ---------- Compute: every field against the fake session ----------

    [Fact]
    public void Compute_FullState_EveryField()
    {
        var crosshair = new CrosshairState { IsInsidePlot = true, SnapIndex = 50 };
        var tick = TickAt(Now.AddSeconds(-5), 1.10000, 1.10010);

        var s = HudDataProvider.Compute(MakeInput(
            plotWidth: 1000,
            crosshair: crosshair,
            tick: tick,
            advanced: HudField.Fps | HudField.ZoomLevel));

        Assert.Equal("EURUSD", s.Symbol);
        Assert.Equal(Timeframe.M15, s.Timeframe);
        Assert.Equal(HudPresentation.Full, s.Presentation);
        Assert.Equal(HudField.Information | HudField.Fps | HudField.ZoomLevel, s.Visible);

        Assert.Equal(50, s.OhlcIndex);
        Assert.True(s.OhlcUnderMouse.HasValue);
        Assert.Equal(150.0, s.OhlcUnderMouse.GetValueOrDefault().Volume, 9);

        Assert.True(s.SpreadPoints.HasValue);
        Assert.Equal(10, s.SpreadPoints.GetValueOrDefault());
        Assert.Equal(HudMarketStatus.Live, s.MarketStatus);

        Assert.Equal(95, s.VisibleBars);
        Assert.Equal(Start.AddMinutes(5 * 15), s.Start.GetValueOrDefault());
        Assert.Equal(Start.AddMinutes(99 * 15), s.End.GetValueOrDefault());

        Assert.Equal(133, s.ZoomLevelPercent);
        Assert.Equal(100, s.LoadedCandles);
        Assert.Equal("CSV", s.ProviderName);
        Assert.Equal(HudCacheStatus.Memory, s.CacheStatus);
        Assert.Equal(60.0, s.Fps, 9);
    }

    [Fact]
    public void Compute_PointerOutsidePlot_ShowsLatestCandle()
    {
        var crosshair = new CrosshairState { IsInsidePlot = false, SnapIndex = 50 };
        var s = HudDataProvider.Compute(MakeInput(crosshair: crosshair));

        Assert.Equal(99, s.OhlcIndex);
        Assert.Equal(199.0, s.OhlcUnderMouse.GetValueOrDefault().Volume, 9);
    }

    [Fact]
    public void Compute_StartAndEnd_AreInDisplayTimeZone()
    {
        var plus3 = TimeZoneInfo.CreateCustomTimeZone("T3", TimeSpan.FromHours(3), "T3", "T3");
        var s = HudDataProvider.Compute(MakeInput(zone: plus3));

        // bar 5 opens 12:00 + 75 min = 13:15Z -> 16:15 in UTC+3
        var start = s.Start.GetValueOrDefault();
        Assert.Equal(TimeSpan.FromHours(3), start.Offset);
        Assert.Equal(16, start.Hour);
        Assert.Equal(15, start.Minute);
        Assert.Equal(Start.AddMinutes(5 * 15), start);
    }

    [Fact]
    public void Compute_NoTick_SpreadNullAndStale()
    {
        var s = HudDataProvider.Compute(MakeInput(tick: null));

        Assert.False(s.SpreadPoints.HasValue);
        Assert.Equal(HudMarketStatus.Stale, s.MarketStatus);
    }

    [Fact]
    public void Compute_ReplayAndOffline_Statuses()
    {
        var tick = TickAt(Now.AddSeconds(-1), 1.1, 1.1001);

        Assert.Equal(HudMarketStatus.Replay, HudDataProvider.Compute(MakeInput(tick: tick, replay: true)).MarketStatus);
        Assert.Equal(HudMarketStatus.Offline, HudDataProvider.Compute(MakeInput(tick: tick, connected: false)).MarketStatus);
    }

    [Fact]
    public void Compute_CompactAndMinimal_ShowTheirFieldSets()
    {
        var compact = HudDataProvider.Compute(MakeInput(plotWidth: 700, advanced: HudField.Advanced));
        Assert.Equal(HudPresentation.Compact, compact.Presentation);
        Assert.Equal(
            HudField.Symbol | HudField.Timeframe | HudField.OhlcUnderMouse | HudField.Spread,
            compact.Visible);

        var minimal = HudDataProvider.Compute(MakeInput(plotWidth: 400, advanced: HudField.Advanced));
        Assert.Equal(HudPresentation.Minimal, minimal.Presentation);
        Assert.Equal(HudField.Symbol | HudField.Timeframe | HudField.Spread, minimal.Visible);
    }

    [Fact]
    public void Compute_EmptySeries_HasNoCandleAndNoRange()
    {
        var s = HudDataProvider.Compute(MakeInput(
            bars: new List<Candle>(),
            range: new RenderWindowRange(0, -1, 0, -1, 0)));

        Assert.Equal(-1, s.OhlcIndex);
        Assert.False(s.OhlcUnderMouse.HasValue);
        Assert.False(s.Start.HasValue);
        Assert.False(s.End.HasValue);
        Assert.Equal(0, s.LoadedCandles);
        Assert.Equal(0, s.VisibleBars);
    }

    [Fact]
    public void Compute_RangeBeyondSeries_HasNoStartOrEnd()
    {
        var s = HudDataProvider.Compute(MakeInput(range: new RenderWindowRange(0, 150, 0, 150, 151)));

        Assert.False(s.Start.HasValue);
        Assert.False(s.End.HasValue);
    }

    // ---------- FPS ----------

    [Fact]
    public void FpsMeter_AveragesOverOneSecond()
    {
        var meter = new FpsMeter();
        Assert.Equal(0.0, meter.Fps(Start), 9);

        // 50 frames, 20 ms apart: t = 0 .. 980 ms
        for (int i = 0; i < 50; i++)
            meter.RecordFrame(Start.AddMilliseconds(20 * i));

        Assert.Equal(50.0, meter.Fps(Start.AddMilliseconds(980)), 9);

        // the frame at t = 0 is exactly one second old and no longer counts
        Assert.Equal(49.0, meter.Fps(Start.AddMilliseconds(1000)), 9);

        // everything is older than one second
        Assert.Equal(0.0, meter.Fps(Start.AddSeconds(5)), 9);
    }

    // ---------- REFRESH ----------

    [Fact]
    public void Refresh_ThousandMouseMovesInsideOneBar_IsZero()
    {
        var tracker = new HudRefreshTracker();

        // initial draw
        Assert.True(tracker.ShouldRefresh(50, 5, 99, HudMarketStatus.Live));

        int refreshes = 0;
        for (int i = 0; i < 1000; i++)
        {
            // every pixel of the move reports the same snapped bar
            var crosshair = new CrosshairState { IsInsidePlot = true, SnapIndex = 50, X = 400 + (i % 8) };
            int index = HudDataProvider.OhlcIndex(crosshair, 100);
            if (tracker.ShouldRefresh(index, 5, 99, HudMarketStatus.Live))
                refreshes++;
        }

        Assert.Equal(0, refreshes);
    }

    [Fact]
    public void Refresh_HoveredBarChange_RefreshesOnce()
    {
        var tracker = new HudRefreshTracker();
        Assert.True(tracker.ShouldRefresh(50, 5, 99, HudMarketStatus.Live));

        Assert.True(tracker.ShouldRefresh(51, 5, 99, HudMarketStatus.Live));
        Assert.False(tracker.ShouldRefresh(51, 5, 99, HudMarketStatus.Live));
    }

    [Fact]
    public void Refresh_RangeChange_Refreshes()
    {
        var tracker = new HudRefreshTracker();
        Assert.True(tracker.ShouldRefresh(50, 5, 99, HudMarketStatus.Live));

        Assert.True(tracker.ShouldRefresh(50, 6, 99, HudMarketStatus.Live));
        Assert.True(tracker.ShouldRefresh(50, 6, 100, HudMarketStatus.Live));
        Assert.False(tracker.ShouldRefresh(50, 6, 100, HudMarketStatus.Live));
    }

    [Fact]
    public void Refresh_MarketStatusChange_Refreshes()
    {
        var tracker = new HudRefreshTracker();
        Assert.True(tracker.ShouldRefresh(50, 5, 99, HudMarketStatus.Live));

        Assert.True(tracker.ShouldRefresh(50, 5, 99, HudMarketStatus.Stale));
        Assert.False(tracker.ShouldRefresh(50, 5, 99, HudMarketStatus.Stale));
    }

    [Fact]
    public void Refresh_CandleUpdated_IsCoalesced()
    {
        var tracker = new HudRefreshTracker();
        Assert.True(tracker.ShouldRefresh(99, 5, 99, HudMarketStatus.Live));
        Assert.False(tracker.ShouldRefresh(99, 5, 99, HudMarketStatus.Live));

        // three updates before the next draw cause exactly one refresh
        tracker.NotifyCandleUpdated();
        tracker.NotifyCandleUpdated();
        tracker.NotifyCandleUpdated();

        Assert.True(tracker.ShouldRefresh(99, 5, 99, HudMarketStatus.Live));
        Assert.False(tracker.ShouldRefresh(99, 5, 99, HudMarketStatus.Live));
    }

    [Fact]
    public void Refresh_FpsField_RunsAtTwoHertz()
    {
        var tracker = new HudRefreshTracker();

        Assert.True(tracker.ShouldRefreshFps(Start));
        Assert.False(tracker.ShouldRefreshFps(Start.AddMilliseconds(100)));
        Assert.False(tracker.ShouldRefreshFps(Start.AddMilliseconds(499)));
        Assert.True(tracker.ShouldRefreshFps(Start.AddMilliseconds(500)));
        Assert.False(tracker.ShouldRefreshFps(Start.AddMilliseconds(600)));
        Assert.True(tracker.ShouldRefreshFps(Start.AddMilliseconds(1000)));
    }
}
