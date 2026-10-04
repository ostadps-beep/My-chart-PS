using MyChart.Core.Models.Viewport;

namespace MyChart.Core.Scale;

/// <summary>
/// T2.06 — GoToLatest, LatestCandleLock, InfiniteScroll request trigger.
/// </summary>
public sealed class LatestViewController
{
    public bool LatestCandleLock { get; private set; }
    public double LockedRightOffset { get; private set; }
    public bool ReachedStart { get; set; }
    public bool HistoryRequestInFlight { get; set; }

    public const int InfiniteScrollThreshold = 200;
    public const int InfiniteScrollBars = 2000;
    public const int MaxM1BarsPerRequest = 200_000;

    /// <summary>
    /// GoToLatest (Home): RightOffset := plotWidth / (2 * BarSpacing) - 0.5
    /// → latest bar centred. BarSpacing and price scale unchanged.
    /// </summary>
    public static void GoToLatest(ViewState vs)
    {
        if (vs.BarSpacing <= 0) return;
        vs.RightOffset = vs.PlotWidth / (2.0 * vs.BarSpacing) - 0.5;
    }

    public void EnableLock(ViewState vs)
    {
        LatestCandleLock = true;
        LockedRightOffset = vs.RightOffset;
    }

    public void DisableLock()
    {
        LatestCandleLock = false;
    }

    /// <summary>After CandleUpdated/CandleClosed: force RightOffset back if locked.</summary>
    public void OnCandleEvent(ViewState vs)
    {
        if (LatestCandleLock)
            vs.RightOffset = LockedRightOffset;
    }

    /// <summary>Horizontal pan/scroll ignored when lock is ON.</summary>
    public bool AllowHorizontalPan => !LatestCandleLock;

    /// <summary>
    /// when renderFrom &lt;= 200 and !reachedStart and !inFlight → should request older history.
    /// </summary>
    public bool ShouldRequestOlderHistory(int renderFrom)
        => renderFrom <= InfiniteScrollThreshold
           && !ReachedStart
           && !HistoryRequestInFlight;

    public int M1BarsForRequest(TimeframeBarsEstimate estimate)
        => Math.Min(estimate.M1CountForBars(InfiniteScrollBars), MaxM1BarsPerRequest);
}

/// <summary>Helper for converting bar counts to M1 for InfiniteScroll.</summary>
public readonly record struct TimeframeBarsEstimate(int MinutesPerBar)
{
    public int M1CountForBars(int bars) => bars * Math.Max(1, MinutesPerBar);
}
