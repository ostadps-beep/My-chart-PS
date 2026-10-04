using MyChart.Core.Models.Market;

namespace MyChart.Core.Candles;

/// <summary>
/// T1.03 DataValidation — pure functions. Order:
/// Normalization → CorruptedRecords → InvalidOHLC → Duplicate → Sort → TimeGaps → MissingBars.
/// NO_FABRICATION: never invents candles.
/// </summary>
public static class DataValidator
{
    public static (IReadOnlyList<Candle> Candles, ValidationReport Report) Validate(
        IReadOnlyList<Candle> input,
        Timeframe timeframe,
        int digits,
        SessionCalendar calendar,
        DateTimeOffset? nowUtc = null)
    {
        var now = nowUtc ?? DateTimeOffset.UtcNow;
        int corrupted = 0, repaired = 0, duplicates = 0, rejected = 0;

        // 1) Normalization + 2) CorruptedRecords + 3) InvalidOHLC (per row, before dedup)
        var working = new List<(Candle C, bool WasRepaired)>();

        foreach (var raw in input)
        {
            // CorruptedRecords
            if (IsCorrupted(raw, now))
            {
                corrupted++;
                rejected++;
                continue;
            }

            // Normalization: floor timestamp, round prices
            var ts = TimeBuckets.Floor(raw.Timestamp, timeframe, calendar);
            double o = RoundPrice(raw.Open, digits);
            double h = RoundPrice(raw.High, digits);
            double l = RoundPrice(raw.Low, digits);
            double c = RoundPrice(raw.Close, digits);
            double v = raw.Volume;

            var candle = new Candle(ts, o, h, l, c, v);
            bool wasRepaired = false;

            // InvalidOHLC repair
            if (IsInvalidOhlc(candle))
            {
                h = Math.Max(Math.Max(o, c), Math.Max(h, l));
                l = Math.Min(Math.Min(o, c), Math.Min(h, l));
                // re-apply after using original h/l in min/max — use fresh max/min of O,H,L,C
                h = Math.Max(Math.Max(o, candle.High), Math.Max(c, candle.Low));
                l = Math.Min(Math.Min(o, candle.High), Math.Min(c, candle.Low));
                // Correct per spec: High := max(Open,High,Low,Close); Low := min(Open,High,Low,Close)
                h = Max4(o, candle.High, candle.Low, c);
                l = Min4(o, candle.High, candle.Low, c);
                candle = new Candle(ts, o, h, l, c, v);
                wasRepaired = true;
                repaired++;
            }

            working.Add((candle, wasRepaired));
        }

        // 4) DuplicateDetection — same Timestamp keep LAST
        var byTs = new Dictionary<DateTimeOffset, (Candle C, bool WasRepaired)>();
        foreach (var item in working)
        {
            if (byTs.ContainsKey(item.C.Timestamp))
                duplicates++;
            byTs[item.C.Timestamp] = item;
        }

        // 5) Sort ascending
        var accepted = byTs.Values
            .OrderBy(x => x.C.Timestamp)
            .Select(x => x.C)
            .ToList();

        // 6+7) TimeGaps + MissingBars + GapKind
        var gaps = new List<GapInfo>();
        for (int i = 0; i < accepted.Count - 1; i++)
        {
            var t0 = accepted[i].Timestamp;
            var t1 = accepted[i + 1].Timestamp;
            var span = TimeBuckets.Span(t0, timeframe, calendar);
            if (t1 - t0 > span)
            {
                int missing = CountMissingBars(t0, t1, timeframe, calendar);
                var kind = ClassifyGap(t0, t1, calendar);
                gaps.Add(new GapInfo(t0, t1, missing, kind));
            }
        }

        var report = new ValidationReport(
            Accepted: accepted.Count,
            Repaired: repaired,
            Rejected: rejected,
            Duplicates: duplicates,
            Corrupted: corrupted,
            Gaps: gaps);

        return (accepted, report);
    }

    public static bool IsCorrupted(Candle c, DateTimeOffset nowUtc)
    {
        if (double.IsNaN(c.Open) || double.IsInfinity(c.Open) || c.Open <= 0) return true;
        if (double.IsNaN(c.High) || double.IsInfinity(c.High) || c.High <= 0) return true;
        if (double.IsNaN(c.Low) || double.IsInfinity(c.Low) || c.Low <= 0) return true;
        if (double.IsNaN(c.Close) || double.IsInfinity(c.Close) || c.Close <= 0) return true;
        if (double.IsNaN(c.Volume) || c.Volume < 0) return true;
        if (c.Timestamp > nowUtc + TimeSpan.FromMinutes(5)) return true;
        return false;
    }

    public static bool IsInvalidOhlc(Candle c)
        => c.High < Max4(c.Open, c.Close, c.Low, c.High)
           || c.Low > Min4(c.Open, c.Close, c.High, c.Low)
           || c.High < c.Low;

    public static double RoundPrice(double price, int digits)
        => Math.Round(price, digits, MidpointRounding.AwayFromZero);

    public static int CountMissingBars(
        DateTimeOffset from, DateTimeOffset to, Timeframe tf, SessionCalendar calendar)
    {
        // number of timeframe slots strictly between the two neighbours
        int count = 0;
        var cursor = TimeBuckets.Next(from, tf, calendar);
        while (cursor < to)
        {
            count++;
            cursor = TimeBuckets.Next(cursor, tf, calendar);
            if (count > 1_000_000) break; // safety
        }
        return count;
    }

    public static GapKind ClassifyGap(
        DateTimeOffset from, DateTimeOffset to, SessionCalendar calendar)
    {
        var group = calendar.Group;
        var offset = TimeBuckets.OffsetAt(from, calendar);

        // Crypto: never Expected
        if (group == SymbolGroup.Crypto)
        {
            var duration = to - from;
            if (duration > TimeSpan.FromHours(24))
                return GapKind.Unclassified;
            return GapKind.Missing;
        }

        bool hasSaturday = ContainsLocalSaturday(from, to, offset);

        if (group is SymbolGroup.Forex or SymbolGroup.Indices or SymbolGroup.Commodities)
        {
            if (hasSaturday)
                return GapKind.Expected;

            if (group is SymbolGroup.Indices or SymbolGroup.Commodities
                && (to - from) > TimeSpan.FromHours(24)
                && !hasSaturday)
                return GapKind.Unclassified;

            return GapKind.Missing;
        }

        // Stocks
        if (group == SymbolGroup.Stocks)
        {
            if (hasSaturday || (to - from) >= TimeSpan.FromHours(8))
                return GapKind.Expected;

            if ((to - from) > TimeSpan.FromHours(24) && !hasSaturday)
                return GapKind.Unclassified;

            return GapKind.Missing;
        }

        return GapKind.Missing;
    }

    private static bool ContainsLocalSaturday(DateTimeOffset from, DateTimeOffset to, TimeSpan offset)
    {
        // walk local midnights between from and to
        var localFrom = from.ToOffset(offset);
        var localTo = to.ToOffset(offset);
        var day = localFrom.Date;
        var end = localTo.Date;
        while (day <= end)
        {
            if (day.DayOfWeek == DayOfWeek.Saturday)
                return true;
            day = day.AddDays(1);
        }
        return false;
    }

    private static double Max4(double a, double b, double c, double d)
        => Math.Max(Math.Max(a, b), Math.Max(c, d));

    private static double Min4(double a, double b, double c, double d)
        => Math.Min(Math.Min(a, b), Math.Min(c, d));
}
