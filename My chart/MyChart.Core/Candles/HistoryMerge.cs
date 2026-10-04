using MyChart.Core.Models.Market;

namespace MyChart.Core.Candles;

/// <summary>
/// T1.08 HistoryMergeGapRepair — pure merge + gap helpers.
/// Merge: union by Timestamp; on conflict INCOMING wins; result strictly ascending.
/// </summary>
public static class HistoryMerge
{
    public const int DownloadChunkBars = 50_000;
    public const int MaxRepairAttempts = 2;

    /// <summary>
    /// Union by Timestamp; incoming wins on conflict; strictly ascending result.
    /// </summary>
    public static List<Candle> Merge(
        IReadOnlyList<Candle> existing,
        IReadOnlyList<Candle> incoming)
    {
        var map = new Dictionary<DateTimeOffset, Candle>();

        if (existing != null)
        {
            foreach (var c in existing)
                map[c.Timestamp] = c;
        }

        if (incoming != null)
        {
            foreach (var c in incoming)
                map[c.Timestamp] = c; // incoming wins
        }

        return map.Values.OrderBy(c => c.Timestamp).ToList();
    }

    /// <summary>
    /// Detect gaps after merge using T1.03 rules.
    /// </summary>
    public static IReadOnlyList<GapInfo> DetectGaps(
        IReadOnlyList<Candle> series,
        Timeframe timeframe,
        SessionCalendar calendar)
    {
        var gaps = new List<GapInfo>();
        for (int i = 0; i < series.Count - 1; i++)
        {
            var t0 = series[i].Timestamp;
            var t1 = series[i + 1].Timestamp;
            var span = TimeBuckets.Span(t0, timeframe, calendar);
            if (t1 - t0 > span)
            {
                int missing = DataValidator.CountMissingBars(t0, t1, timeframe, calendar);
                var kind = DataValidator.ClassifyGap(t0, t1, calendar);
                gaps.Add(new GapInfo(t0, t1, missing, kind));
            }
        }
        return gaps;
    }

    /// <summary>
    /// For each Missing gap, invoke request once (caller tracks attempts).
    /// Returns gaps still Missing after the provided fill candles are merged.
    /// </summary>
    public static (List<Candle> Series, IReadOnlyList<GapInfo> RemainingMissing) TryRepairMissing(
        IReadOnlyList<Candle> series,
        IReadOnlyList<Candle> repairChunk,
        Timeframe timeframe,
        SessionCalendar calendar)
    {
        var merged = Merge(series, repairChunk);
        var gaps = DetectGaps(merged, timeframe, calendar)
            .Where(g => g.Kind == GapKind.Missing)
            .ToList();
        return (merged, gaps);
    }

    /// <summary>
    /// Update window: re-request from (last stored timestamp - 2 bars) to now.
    /// </summary>
    public static DateTimeOffset UpdateFrom(DateTimeOffset lastStored, Timeframe timeframe, SessionCalendar calendar)
    {
        var t = lastStored;
        t = TimeBuckets.Prev(t, timeframe, calendar);
        t = TimeBuckets.Prev(t, timeframe, calendar);
        return t;
    }
}
