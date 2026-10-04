using MyChart.Core.Analysis;
using MyChart.Core.Models.Market;
using MyChart.Core.Models.Viewport;
using MyChart.Core.Scale;
using Xunit;

namespace MyChart.Tests.Analysis;

/// <summary>
/// T3.03 NavigatorCalculations. Fixture: N=100 M15 bars from 2024-03-13T12:00Z (bar i opens 12:00 + 15 min * i),
/// plotLeft 0, plotWidth 800, BarSpacing 8, so the plot centre is at X = 400.
/// </summary>
public class NavigatorTests
{
    private static readonly DateTimeOffset Start = new(2024, 3, 13, 12, 0, 0, TimeSpan.Zero);
    private static readonly SessionCalendar Calendar = new(ServerTimeRule.Utc, SymbolGroup.Forex);

    private static ViewState MakeView() => new()
    {
        Width = 864,
        Height = 524,
        PriceAxisWidth = 64,
        TimeAxisHeight = 24,
        BarSpacing = 8,
        RightOffset = 5
    };

    private static TimeIndexMapper MakeMapper(int n = 100)
    {
        var opens = new List<DateTimeOffset>();
        for (int i = 0; i < n; i++)
            opens.Add(Start.AddMinutes(15 * i));
        return new TimeIndexMapper(opens, Timeframe.M15, Calendar);
    }

    private static List<Candle> MakeBars(int n)
    {
        var bars = new List<Candle>();
        for (int i = 0; i < n; i++)
        {
            double close = 1.1000 + i * 0.0001;
            bars.Add(new Candle(Start.AddMinutes(15 * i), close, close + 0.0005, close - 0.0005, close, 1));
        }

        return bars;
    }

    private static Candle CloseOnly(int index, double close)
        => new(Start.AddMinutes(15 * index), close, close, close, close, 1);

    // ---------- CenterOn ----------

    [Fact]
    public void CenterOn_PutsXOfUAtPlotCentre()
    {
        var vs = MakeView();
        var conv = new CoordinateConverter(vs, 100);

        foreach (double u in new[] { 0.0, 25.5, 50.0, 99.0 })
        {
            NavigatorCalculator.CenterOn(vs, 100, u);
            Assert.Equal(vs.PlotLeft + vs.PlotWidth / 2, conv.X(u), 9);
        }
    }

    [Fact]
    public void CenterOn_RightOffsetFormula()
    {
        var vs = MakeView();

        // u - (N-1) - 0.5 + plotWidth / (2 * BarSpacing) = 50 - 99 - 0.5 + 50
        NavigatorCalculator.CenterOn(vs, 100, 50);
        Assert.Equal(0.5, vs.RightOffset, 9);
    }

    [Fact]
    public void CenterOn_IsClampedByScrollLimits()
    {
        var vs = MakeView();

        // upper limit = plotWidth / BarSpacing - 2 = 98 ; lower limit = 2 - N = -98
        NavigatorCalculator.CenterOn(vs, 100, 1000);
        Assert.Equal(98.0, vs.RightOffset, 9);

        NavigatorCalculator.CenterOn(vs, 100, -1000);
        Assert.Equal(-98.0, vs.RightOffset, 9);
    }

    [Fact]
    public void CenterOn_EmptySeries_ChangesNothing()
    {
        var vs = MakeView();
        NavigatorCalculator.CenterOn(vs, 0, 10);
        Assert.Equal(5.0, vs.RightOffset, 9);
    }

    // ---------- GoToCandle ----------

    [Fact]
    public void GoToCandle_Zero_EqualsGoToLatest()
    {
        var viaCandle = MakeView();
        var viaLatest = MakeView();

        var result = NavigatorCalculator.GoToCandle(viaCandle, 100, 0);
        LatestViewController.GoToLatest(viaLatest);

        Assert.Equal(NavigateOutcome.Centered, result.Outcome);
        Assert.Equal(99, result.TargetIndex);
        Assert.Equal(viaLatest.RightOffset, viaCandle.RightOffset, 9);
        Assert.Equal(49.5, viaCandle.RightOffset, 9);
    }

    [Fact]
    public void GoToCandle_CountsBarsBackFromLatest()
    {
        var vs = MakeView();
        var conv = new CoordinateConverter(vs, 100);

        var result = NavigatorCalculator.GoToCandle(vs, 100, 10);

        Assert.Equal(89, result.TargetIndex);
        Assert.Equal(39.5, vs.RightOffset, 9);
        Assert.Equal(400.0, conv.X(89), 9);
    }

    [Fact]
    public void GoToCandle_ClampsToSeries()
    {
        var vs = MakeView();

        Assert.Equal(0, NavigatorCalculator.GoToCandle(vs, 100, 1000).TargetIndex);
        Assert.Equal(99, NavigatorCalculator.GoToCandle(vs, 100, -5).TargetIndex);
        Assert.Equal(NavigateOutcome.NoData, NavigatorCalculator.GoToCandle(vs, 0, 3).Outcome);
    }

    // ---------- GoToDate / GoToTime ----------

    [Fact]
    public void GoToDate_UsesLocalMidnightOfTheDisplayZone()
    {
        var mapper = MakeMapper();
        var date = new DateOnly(2024, 3, 14);

        // UTC: 2024-03-14T00:00Z = start + 12 h = bar 48
        var vsUtc = MakeView();
        var utc = NavigatorCalculator.GoToDate(vsUtc, mapper, date, null, true, false);
        Assert.Equal(NavigateOutcome.Centered, utc.Outcome);
        Assert.Equal(48, utc.TargetIndex);
        Assert.Equal(400.0, new CoordinateConverter(vsUtc, 100).X(48), 9);

        // UTC+3: local midnight = 2024-03-13T21:00Z = start + 9 h = bar 36
        var plus3 = TimeZoneInfo.CreateCustomTimeZone("T3", TimeSpan.FromHours(3), "T3", "T3");
        var shifted = NavigatorCalculator.GoToDate(MakeView(), mapper, date, plus3, true, false);
        Assert.Equal(36, shifted.TargetIndex);
    }

    [Fact]
    public void GoToTime_SnapsToTheNearestBarCentre()
    {
        var mapper = MakeMapper();

        // bar 4 opens at 13:00Z
        Assert.Equal(4, NavigatorCalculator.GoToTime(MakeView(), mapper, new DateTime(2024, 3, 13, 13, 0, 0), null, true, false).TargetIndex);

        // 13:07 -> u 4.47 -> bar 4 ; 13:08 -> u 4.53 -> bar 5
        Assert.Equal(4, NavigatorCalculator.GoToTime(MakeView(), mapper, new DateTime(2024, 3, 13, 13, 7, 0), null, true, false).TargetIndex);
        Assert.Equal(5, NavigatorCalculator.GoToTime(MakeView(), mapper, new DateTime(2024, 3, 13, 13, 8, 0), null, true, false).TargetIndex);
    }

    [Fact]
    public void GoToTime_InsideLastBar_CentresTheLastBar()
    {
        var mapper = MakeMapper();

        // last bar opens 2024-03-14T12:45Z; 12:53 is inside it (u 99.53) and must not centre a future slot
        var result = NavigatorCalculator.GoToTime(MakeView(), mapper, new DateTime(2024, 3, 14, 12, 53, 0), null, true, false);

        Assert.Equal(NavigateOutcome.Centered, result.Outcome);
        Assert.Equal(99, result.TargetIndex);
    }

    [Fact]
    public void GoToDate_AfterLastBar_GoesToLatest()
    {
        var mapper = MakeMapper();

        var vs = MakeView();
        var result = NavigatorCalculator.GoToDate(vs, mapper, new DateOnly(2024, 3, 20), null, true, false);
        Assert.Equal(NavigateOutcome.WentToLatest, result.Outcome);
        Assert.Equal(99, result.TargetIndex);
        Assert.Equal(49.5, vs.RightOffset, 9);

        // exactly at the end of the last bar (13:00Z) is already after it
        var atEnd = MakeView();
        var end = NavigatorCalculator.GoToTimeUtc(atEnd, mapper, new DateTimeOffset(2024, 3, 14, 13, 0, 0, TimeSpan.Zero), true, false);
        Assert.Equal(NavigateOutcome.WentToLatest, end.Outcome);
        Assert.Equal(49.5, atEnd.RightOffset, 9);
    }

    [Fact]
    public void GoToDate_BeforeFirstBar_RequestsHistoryOnceThenGoesToBarZero()
    {
        var mapper = MakeMapper();
        var date = new DateOnly(2024, 3, 12);

        // 1) start of history not reached and nothing requested yet: ask the caller to request history, view unchanged
        var vs1 = MakeView();
        var first = NavigatorCalculator.GoToDate(vs1, mapper, date, null, false, false);
        Assert.Equal(NavigateOutcome.NeedsOlderHistory, first.Outcome);
        Assert.Equal(-1, first.TargetIndex);
        Assert.Equal(5.0, vs1.RightOffset, 9);

        // 2) retry after the request: still before the first bar -> bar 0
        var vs2 = MakeView();
        var retry = NavigatorCalculator.GoToDate(vs2, mapper, date, null, false, true);
        Assert.Equal(NavigateOutcome.ClampedToFirstBar, retry.Outcome);
        Assert.Equal(0, retry.TargetIndex);
        Assert.Equal(-49.5, vs2.RightOffset, 9);

        // 3) start of history already reached: no request, bar 0
        var vs3 = MakeView();
        var reached = NavigatorCalculator.GoToDate(vs3, mapper, date, null, true, false);
        Assert.Equal(NavigateOutcome.ClampedToFirstBar, reached.Outcome);
        Assert.Equal(0, reached.TargetIndex);
    }

    [Fact]
    public void GoToTime_EmptySeries_IsNoData()
    {
        var vs = MakeView();
        var result = NavigatorCalculator.GoToTimeUtc(vs, MakeMapper(0), Start, true, false);

        Assert.Equal(NavigateOutcome.NoData, result.Outcome);
        Assert.Equal(5.0, vs.RightOffset, 9);
    }

    [Fact]
    public void LocalToUtc_ConvertsAndHandlesUnspecifiedKind()
    {
        var plus3 = TimeZoneInfo.CreateCustomTimeZone("T3", TimeSpan.FromHours(3), "T3", "T3");

        var local = new DateTime(2024, 3, 14, 0, 0, 0, DateTimeKind.Local);
        var utc = NavigatorCalculator.LocalToUtc(local, plus3);

        Assert.Equal(new DateTimeOffset(2024, 3, 13, 21, 0, 0, TimeSpan.Zero), utc);
    }

    // ---------- VisibleRangeBox ----------

    [Fact]
    public void VisibleBox_IsFractionOfThePanelWidth()
    {
        var full = NavigatorCalculator.VisibleBox(new RenderWindowRange(5, 99, 4, 99, 95), 100, 1000);
        Assert.Equal(50.0, full.Left, 9);
        Assert.Equal(1000.0, full.Right, 9);

        var middle = NavigatorCalculator.VisibleBox(new RenderWindowRange(30, 69, 29, 70, 40), 100, 500);
        Assert.Equal(150.0, middle.Left, 9);
        Assert.Equal(350.0, middle.Right, 9);

        var empty = NavigatorCalculator.VisibleBox(new RenderWindowRange(0, -1, 0, -1, 0), 100, 500);
        Assert.Equal(0.0, empty.Left, 9);
        Assert.Equal(0.0, empty.Right, 9);
    }

    [Fact]
    public void CenterOnBox_UsesTheBoxCentre()
    {
        var vs = MakeView();
        var conv = new CoordinateConverter(vs, 100);
        var box = new VisibleRangeBox(50, 1000);

        // centre 525 of 1000 -> u = 52.5 - 0.5 = 52 (the middle of bars 5..99)
        Assert.Equal(52.0, NavigatorCalculator.IndexAtPanelX(525, 1000, 100), 9);

        NavigatorCalculator.CenterOnBox(vs, 100, box, 1000);

        Assert.Equal(2.5, vs.RightOffset, 9);
        Assert.Equal(400.0, conv.X(52), 9);
    }

    // ---------- RangeStatistics ----------

    [Fact]
    public void RangeStatistics_OfTheVisibleBars()
    {
        var bars = MakeBars(100);
        var range = new RenderWindowRange(5, 99, 4, 99, 95);

        var stats = NavigatorCalculator.ComputeRangeStatistics(bars, range);

        Assert.True(stats.HasValue);
        var s = stats.GetValueOrDefault();
        Assert.Equal(1.1104, s.High, 9);
        Assert.Equal(1.1000, s.Low, 9);
        Assert.Equal((1.1099 / 1.1005 - 1.0) * 100.0, s.ChangePercent, 9);
        Assert.Equal(95, s.BarCount);
        Assert.Equal(Start.AddMinutes(5 * 15), s.Start);
        Assert.Equal(Start.AddMinutes(99 * 15), s.End);
    }

    [Fact]
    public void RangeStatistics_StartAndEnd_AreInDisplayTimeZone()
    {
        var plus3 = TimeZoneInfo.CreateCustomTimeZone("T3", TimeSpan.FromHours(3), "T3", "T3");
        var stats = NavigatorCalculator.ComputeRangeStatistics(MakeBars(100), new RenderWindowRange(5, 99, 4, 99, 95), plus3);

        Assert.Equal(TimeSpan.FromHours(3), stats.GetValueOrDefault().Start.Offset);
        Assert.Equal(TimeSpan.FromHours(3), stats.GetValueOrDefault().End.Offset);
    }

    [Fact]
    public void RangeStatistics_NoVisibleBars_IsNull()
    {
        var bars = MakeBars(100);

        Assert.False(NavigatorCalculator.ComputeRangeStatistics(bars, new RenderWindowRange(0, -1, 0, -1, 0)).HasValue);
        Assert.False(NavigatorCalculator.ComputeRangeStatistics(bars, new RenderWindowRange(0, 150, 0, 150, 151)).HasValue);
        Assert.False(NavigatorCalculator.ComputeRangeStatistics(new List<Candle>(), new RenderWindowRange(0, 0, 0, 0, 1)).HasValue);
    }

    [Fact]
    public void RangeStatistics_ZeroFirstClose_ChangeIsNaN()
    {
        var bars = new List<Candle> { CloseOnly(0, 0.0), CloseOnly(1, 1.0) };

        var stats = NavigatorCalculator.ComputeRangeStatistics(bars, new RenderWindowRange(0, 1, 0, 1, 2));

        Assert.True(stats.HasValue);
        Assert.True(double.IsNaN(stats.GetValueOrDefault().ChangePercent));
    }

    // ---------- CurrentPosition and panel height ----------

    [Fact]
    public void CurrentPosition_IsToPlusOneOverNInPercent()
    {
        Assert.Equal(100.0, NavigatorCalculator.CurrentPositionPercent(new RenderWindowRange(5, 99, 4, 99, 95), 100), 9);
        Assert.Equal(50.0, NavigatorCalculator.CurrentPositionPercent(new RenderWindowRange(0, 49, 0, 50, 50), 100), 9);
        Assert.Equal(0.0, NavigatorCalculator.CurrentPositionPercent(new RenderWindowRange(0, -1, 0, -1, 0), 0), 9);
    }

    [Fact]
    public void PanelHeight_TenPercentClampedTo48And120()
    {
        Assert.Equal(48.0, NavigatorCalculator.PanelHeight(400), 9);
        Assert.Equal(48.0, NavigatorCalculator.PanelHeight(480), 9);
        Assert.Equal(80.0, NavigatorCalculator.PanelHeight(800), 9);
        Assert.Equal(100.0, NavigatorCalculator.PanelHeight(1000), 9);
        Assert.Equal(120.0, NavigatorCalculator.PanelHeight(2000), 9);
    }

    // ---------- Overview ----------

    [Fact]
    public void Overview_MinMaxOfCloseOverEqualGroups()
    {
        var bars = new List<Candle>();
        for (int i = 0; i < 10; i++)
            bars.Add(CloseOnly(i, i + 1));

        var cols = NavigatorOverview.Compute(bars, 5);

        Assert.Equal(5, cols.Length);
        double[] mins = { 1, 3, 5, 7, 9 };
        double[] maxs = { 2, 4, 6, 8, 10 };
        for (int c = 0; c < 5; c++)
        {
            Assert.Equal(mins[c], cols[c].Min, 9);
            Assert.Equal(maxs[c], cols[c].Max, 9);
        }
    }

    [Fact]
    public void Overview_UnevenGroups_UseIntegerBoundaries()
    {
        var bars = new List<Candle>();
        for (int i = 0; i < 10; i++)
            bars.Add(CloseOnly(i, i + 1));

        // columns 3 over 10 bars: [0,3) [3,6) [6,10)
        var cols = NavigatorOverview.Compute(bars, 3);

        Assert.Equal(1.0, cols[0].Min, 9);
        Assert.Equal(3.0, cols[0].Max, 9);
        Assert.Equal(4.0, cols[1].Min, 9);
        Assert.Equal(6.0, cols[1].Max, 9);
        Assert.Equal(7.0, cols[2].Min, 9);
        Assert.Equal(10.0, cols[2].Max, 9);
    }

    [Fact]
    public void Overview_UsesCloseNotHighLow_AndHandlesNonMonotonicData()
    {
        double[] closes = { 3, 1, 2, 5, 4, 6 };
        var bars = new List<Candle>();
        for (int i = 0; i < closes.Length; i++)
            bars.Add(new Candle(Start.AddMinutes(15 * i), closes[i], closes[i] + 100, closes[i] - 100, closes[i], 1));

        var cols = NavigatorOverview.Compute(bars, 2);

        Assert.Equal(1.0, cols[0].Min, 9);
        Assert.Equal(3.0, cols[0].Max, 9);
        Assert.Equal(4.0, cols[1].Min, 9);
        Assert.Equal(6.0, cols[1].Max, 9);
    }

    [Fact]
    public void Overview_MoreColumnsThanBars_RepeatsTheBar()
    {
        var bars = new List<Candle>();
        for (int i = 0; i < 10; i++)
            bars.Add(CloseOnly(i, i + 1));

        var cols = NavigatorOverview.Compute(bars, 20);

        Assert.Equal(20, cols.Length);
        for (int c = 0; c < 20; c++)
        {
            double expected = c / 2 + 1;
            Assert.Equal(expected, cols[c].Min, 9);
            Assert.Equal(expected, cols[c].Max, 9);
        }
    }

    [Fact]
    public void Overview_EmptyInputs_GiveEmptyArray()
    {
        Assert.Empty(NavigatorOverview.Compute(new List<Candle>(), 10));
        Assert.Empty(NavigatorOverview.Compute(MakeBars(10), 0));
    }

    // ---------- Panel auto-hide ----------

    [Fact]
    public void Visibility_DefaultHidden_ShownThenAutoHidesAfterFiveSeconds()
    {
        var v = new NavigatorVisibility();
        Assert.False(v.IsVisible(Start));

        v.Show(Start);
        Assert.True(v.IsVisible(Start));
        Assert.True(v.IsVisible(Start.AddMilliseconds(4999)));
        Assert.False(v.IsVisible(Start.AddSeconds(5)));
    }

    [Fact]
    public void Visibility_InteractionRestartsTheTimer()
    {
        var v = new NavigatorVisibility();
        v.Show(Start);

        v.Interact(Start.AddSeconds(4));
        Assert.True(v.IsVisible(Start.AddMilliseconds(8999)));
        Assert.False(v.IsVisible(Start.AddSeconds(9)));
    }

    [Fact]
    public void Visibility_InteractionWhileHiddenDoesNotShow()
    {
        var v = new NavigatorVisibility();
        v.Interact(Start);
        Assert.False(v.IsVisible(Start));

        v.Show(Start);
        Assert.False(v.IsVisible(Start.AddSeconds(6)));

        // expired: an interaction does not revive it, only Show does
        v.Interact(Start.AddSeconds(6));
        Assert.False(v.IsVisible(Start.AddSeconds(6)));

        v.Show(Start.AddSeconds(6));
        Assert.True(v.IsVisible(Start.AddSeconds(7)));
    }

    [Fact]
    public void Visibility_HideIsImmediate()
    {
        var v = new NavigatorVisibility();
        v.Show(Start);
        v.Hide();
        Assert.False(v.IsVisible(Start.AddSeconds(1)));
    }
}
