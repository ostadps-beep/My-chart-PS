using MyChart.Core.Models.Market;
using MyChart.Core.Models.Viewport;
using MyChart.Core.Scale;

namespace MyChart.Core.Analysis;

/// <summary>What a GoTo* call did.</summary>
public enum NavigateOutcome
{
    /// <summary>The view was centred on TargetIndex.</summary>
    Centered,

    /// <summary>The target is before the first bar and the start of history is known: bar 0 was centred.</summary>
    ClampedToFirstBar,

    /// <summary>The target is after the last bar: GoToLatest was applied.</summary>
    WentToLatest,

    /// <summary>
    /// The target is before the first bar and the start of history is not reached yet.
    /// Nothing changed: the caller requests older history once and calls again with historyRequested = true.
    /// </summary>
    NeedsOlderHistory,

    /// <summary>The series is empty; nothing changed.</summary>
    NoData
}

/// <summary>Result of a GoTo* call. TargetIndex is -1 when nothing was centred.</summary>
public readonly record struct NavigateResult(NavigateOutcome Outcome, int TargetIndex);

/// <summary>Visible-range box of the navigator panel in DIP (Left and Right inside the panel width).</summary>
public readonly record struct VisibleRangeBox(double Left, double Right);

/// <summary>
/// T3.03 RangeStatistics of the visible bars. ChangePercent = (Close[to] / Close[from] - 1) * 100
/// (NaN when Close[from] is 0). Start and End are display-time bar opens.
/// </summary>
public readonly record struct RangeStatistics(
    double High,
    double Low,
    double ChangePercent,
    int BarCount,
    DateTimeOffset Start,
    DateTimeOffset End);

/// <summary>
/// T3.03 NavigatorCalculations — CenterOn, GoToDate / GoToTime / GoToCandle, VisibleRangeBox,
/// RangeStatistics, CurrentPosition and the panel height. Pure: no IO, no UI state.
/// </summary>
public static class NavigatorCalculator
{
    public const double PanelHeightRatio = 0.10;
    public const double PanelMinHeightDip = 48;
    public const double PanelMaxHeightDip = 120;

    /// <summary>
    /// CenterOn(u): RightOffset = u - (N-1) - 0.5 + plotWidth / (2 * BarSpacing), then ScrollLimits.
    /// Afterwards X(u) = plotLeft + plotWidth / 2 unless the scroll limits clamp it.
    /// </summary>
    public static void CenterOn(ViewState vs, int n, double u)
    {
        if (n <= 0 || vs.BarSpacing <= 0) return;

        vs.RightOffset = u - (n - 1) - 0.5 + vs.PlotWidth / (2.0 * vs.BarSpacing);
        vs.RightOffset = PanEngine.ClampRightOffset(vs, n);
    }

    /// <summary>
    /// GoToCandle(n) = n bars back from the latest (0 = latest): u = N-1-n, clamped to [0, N-1].
    /// </summary>
    public static NavigateResult GoToCandle(ViewState vs, int n, int barsBack)
    {
        if (n <= 0) return new NavigateResult(NavigateOutcome.NoData, -1);

        long raw = (long)(n - 1) - barsBack;
        int index = (int)Math.Clamp(raw, 0L, (long)(n - 1));
        CenterOn(vs, n, index);
        return new NavigateResult(NavigateOutcome.Centered, index);
    }

    /// <summary>
    /// GoToDate(date) = local 00:00 of that date in the display time zone (UTC when null) -> UTC -> GoToTimeUtc.
    /// </summary>
    public static NavigateResult GoToDate(
        ViewState vs,
        TimeIndexMapper mapper,
        DateOnly date,
        TimeZoneInfo? displayZone,
        bool reachedStart,
        bool historyRequested)
        => GoToTime(vs, mapper, date.ToDateTime(TimeOnly.MinValue), displayZone, reachedStart, historyRequested);

    /// <summary>GoToTime(dateTime) = the same with the given local time in the display time zone.</summary>
    public static NavigateResult GoToTime(
        ViewState vs,
        TimeIndexMapper mapper,
        DateTime localDateTime,
        TimeZoneInfo? displayZone,
        bool reachedStart,
        bool historyRequested)
        => GoToTimeUtc(vs, mapper, LocalToUtc(localDateTime, displayZone), reachedStart, historyRequested);

    /// <summary>
    /// u = IndexOfTime(t) -> CenterOn(SnapIndex(u)).
    /// Before the first bar: if the start of history is not reached and no request was made yet the result is
    /// NeedsOlderHistory (the caller requests history once and retries once); otherwise bar 0 is centred.
    /// After the last bar (u &gt;= N): GoToLatest. A time inside a bar is centred on that bar (SnapIndex clamped to [0, N-1]).
    /// </summary>
    public static NavigateResult GoToTimeUtc(
        ViewState vs,
        TimeIndexMapper mapper,
        DateTimeOffset utc,
        bool reachedStart,
        bool historyRequested)
    {
        int n = mapper.N;
        if (n == 0) return new NavigateResult(NavigateOutcome.NoData, -1);

        double u = mapper.IndexOfTime(utc);

        if (u < 0)
        {
            if (!reachedStart && !historyRequested)
                return new NavigateResult(NavigateOutcome.NeedsOlderHistory, -1);

            CenterOn(vs, n, 0);
            return new NavigateResult(NavigateOutcome.ClampedToFirstBar, 0);
        }

        if (u >= n)
        {
            LatestViewController.GoToLatest(vs);
            return new NavigateResult(NavigateOutcome.WentToLatest, n - 1);
        }

        int index = Math.Clamp(mapper.SnapIndex(u), 0, n - 1);
        CenterOn(vs, n, index);
        return new NavigateResult(NavigateOutcome.Centered, index);
    }

    /// <summary>VisibleRangeBox: left = from / N ; right = (to + 1) / N of the panel width.</summary>
    public static VisibleRangeBox VisibleBox(RenderWindowRange range, int n, double panelWidth)
    {
        if (n <= 0 || range.VisibleBarCount <= 0) return new VisibleRangeBox(0, 0);

        return new VisibleRangeBox(
            range.From / (double)n * panelWidth,
            (range.To + 1) / (double)n * panelWidth);
    }

    /// <summary>
    /// A panel position (DIP from the panel's left edge) -> u. Bar i occupies [i/N, (i+1)/N] of the panel width,
    /// so its centre is at (i + 0.5) / N and u = x / panelWidth * N - 0.5.
    /// </summary>
    public static double IndexAtPanelX(double panelX, double panelWidth, int n)
        => panelWidth <= 0 ? 0 : panelX / panelWidth * n - 0.5;

    /// <summary>Dragging the box sets CenterOn from the box centre.</summary>
    public static void CenterOnBox(ViewState vs, int n, VisibleRangeBox box, double panelWidth)
    {
        if (n <= 0 || panelWidth <= 0) return;

        double centreX = (box.Left + box.Right) / 2.0;
        CenterOn(vs, n, IndexAtPanelX(centreX, panelWidth, n));
    }

    /// <summary>RangeStatistics of the visible bars; null when there are no visible bars.</summary>
    public static RangeStatistics? ComputeRangeStatistics(
        IReadOnlyList<Candle> bars,
        RenderWindowRange range,
        TimeZoneInfo? displayZone = null)
    {
        if (range.VisibleBarCount <= 0
            || range.From < 0
            || range.To < range.From
            || range.To > bars.Count - 1)
        {
            return null;
        }

        double high = double.MinValue;
        double low = double.MaxValue;
        for (int i = range.From; i <= range.To; i++)
        {
            if (bars[i].High > high) high = bars[i].High;
            if (bars[i].Low < low) low = bars[i].Low;
        }

        double first = bars[range.From].Close;
        double last = bars[range.To].Close;
        double change = first == 0 ? double.NaN : (last / first - 1.0) * 100.0;

        var zone = displayZone ?? TimeZoneInfo.Utc;
        return new RangeStatistics(
            high,
            low,
            change,
            range.To - range.From + 1,
            TimeZoneInfo.ConvertTime(bars[range.From].Timestamp, zone),
            TimeZoneInfo.ConvertTime(bars[range.To].Timestamp, zone));
    }

    /// <summary>CurrentPosition = (to + 1) / N in percent (0 when the series is empty).</summary>
    public static double CurrentPositionPercent(RenderWindowRange range, int n)
        => n <= 0 ? 0 : (range.To + 1) / (double)n * 100.0;

    /// <summary>Panel height: 10 percent of the chart height clamped to [48, 120] DIP.</summary>
    public static double PanelHeight(double chartHeight)
        => Math.Clamp(chartHeight * PanelHeightRatio, PanelMinHeightDip, PanelMaxHeightDip);

    /// <summary>
    /// Local time in the display zone to UTC. A local time that does not exist (daylight-saving gap)
    /// moves forward in 15-minute steps to the first valid time.
    /// </summary>
    public static DateTimeOffset LocalToUtc(DateTime local, TimeZoneInfo? displayZone)
    {
        var zone = displayZone ?? TimeZoneInfo.Utc;
        var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);

        while (zone.IsInvalidTime(unspecified))
            unspecified = unspecified.AddMinutes(15);

        var utc = TimeZoneInfo.ConvertTimeToUtc(unspecified, zone);
        return new DateTimeOffset(utc, TimeSpan.Zero);
    }
}
