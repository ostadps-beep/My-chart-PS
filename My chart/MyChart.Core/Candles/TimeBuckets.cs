using MyChart.Core.Models.Market;

namespace MyChart.Core.Candles;

/// <summary>
/// T1.02 TimeBuckets — pure bucket floor / next / prev / span. No IO, no pixels.
/// </summary>
public static class TimeBuckets
{
    public static TimeSpan OffsetAt(DateTimeOffset utc, SessionCalendar calendar)
    {
        return calendar.Rule switch
        {
            ServerTimeRule.Fixed f => f.Offset,
            ServerTimeRule.EetUsDst => EetUsDstOffset(utc),
            _ => TimeSpan.Zero
        };
    }

    /// <summary>
    /// Floor timestamp <paramref name="t"/> to the open of its timeframe bucket (UTC result).
    /// </summary>
    public static DateTimeOffset Floor(DateTimeOffset t, Timeframe tf, SessionCalendar calendar)
    {
        if (tf == Timeframe.Tick)
            return t;

        var offset = OffsetAt(t, calendar);
        var local = t.ToOffset(offset);

        DateTimeOffset bucketLocal = tf switch
        {
            Timeframe.M1 => FloorToMinute(local, 1),
            Timeframe.M5 => FloorToMinute(local, 5),
            Timeframe.M15 => FloorToMinute(local, 15),
            Timeframe.M30 => FloorToMinute(local, 30),
            Timeframe.H1 => FloorToHour(local, 1),
            Timeframe.H4 => FloorToHour(local, 4),
            Timeframe.D1 => new DateTimeOffset(local.Year, local.Month, local.Day, 0, 0, 0, offset),
            Timeframe.W1 => FloorWeek(local, calendar.Group, offset),
            Timeframe.MN1 => new DateTimeOffset(local.Year, local.Month, 1, 0, 0, 0, offset),
            _ => local
        };

        // result = bucketLocal - OffsetAt(t)  → UTC instant of bucket open
        return bucketLocal.ToUniversalTime();
    }

    public static DateTimeOffset Next(DateTimeOffset bucketStartUtc, Timeframe tf, SessionCalendar calendar)
    {
        if (tf == Timeframe.Tick)
            return bucketStartUtc;

        var offset = OffsetAt(bucketStartUtc, calendar);
        var local = bucketStartUtc.ToOffset(offset);

        DateTimeOffset nextLocal = tf switch
        {
            Timeframe.M1 => local.AddMinutes(1),
            Timeframe.M5 => local.AddMinutes(5),
            Timeframe.M15 => local.AddMinutes(15),
            Timeframe.M30 => local.AddMinutes(30),
            Timeframe.H1 => local.AddHours(1),
            Timeframe.H4 => local.AddHours(4),
            Timeframe.D1 => local.AddDays(1),
            Timeframe.W1 => local.AddDays(7),
            Timeframe.MN1 => local.AddMonths(1),
            _ => local
        };

        return nextLocal.ToUniversalTime();
    }

    public static DateTimeOffset Prev(DateTimeOffset bucketStartUtc, Timeframe tf, SessionCalendar calendar)
    {
        if (tf == Timeframe.Tick)
            return bucketStartUtc;

        var offset = OffsetAt(bucketStartUtc, calendar);
        var local = bucketStartUtc.ToOffset(offset);

        DateTimeOffset prevLocal = tf switch
        {
            Timeframe.M1 => local.AddMinutes(-1),
            Timeframe.M5 => local.AddMinutes(-5),
            Timeframe.M15 => local.AddMinutes(-15),
            Timeframe.M30 => local.AddMinutes(-30),
            Timeframe.H1 => local.AddHours(-1),
            Timeframe.H4 => local.AddHours(-4),
            Timeframe.D1 => local.AddDays(-1),
            Timeframe.W1 => local.AddDays(-7),
            Timeframe.MN1 => local.AddMonths(-1),
            _ => local
        };

        return prevLocal.ToUniversalTime();
    }

    public static TimeSpan Span(DateTimeOffset bucketStartUtc, Timeframe tf, SessionCalendar calendar)
        => Next(bucketStartUtc, tf, calendar) - bucketStartUtc;

    // ── helpers ──────────────────────────────────────────────

    private static DateTimeOffset FloorToMinute(DateTimeOffset local, int minutes)
    {
        var m = local.Minute / minutes * minutes;
        return new DateTimeOffset(local.Year, local.Month, local.Day, local.Hour, m, 0, local.Offset);
    }

    private static DateTimeOffset FloorToHour(DateTimeOffset local, int hours)
    {
        var h = local.Hour / hours * hours;
        return new DateTimeOffset(local.Year, local.Month, local.Day, h, 0, 0, local.Offset);
    }

    /// <summary>
    /// W1 = local Monday 00:00.
    /// Sunday belongs to NEXT week for Forex/Indices/Commodities;
    /// to PREVIOUS Monday for Crypto/Stocks.
    /// </summary>
    private static DateTimeOffset FloorWeek(DateTimeOffset local, SymbolGroup group, TimeSpan offset)
    {
        // DayOfWeek: Sunday=0 ... Saturday=6
        int dow = (int)local.DayOfWeek; // 0=Sun

        bool sundayGoesToNext =
            group is SymbolGroup.Forex or SymbolGroup.Indices or SymbolGroup.Commodities;

        int daysBack;
        if (dow == 0) // Sunday
        {
            daysBack = sundayGoesToNext ? -1 : 6; // -1 => next Monday (+1 day from Sunday)
            // if next: Monday is +1 day from Sunday → daysBack = -1 means add 1
            // if prev: Monday was 6 days ago
        }
        else
        {
            // Mon=1 → 0 days back; Tue=2 → 1; ... Sat=6 → 5
            daysBack = dow - 1;
        }

        var monday = local.Date.AddDays(dow == 0 && sundayGoesToNext ? 1 : -daysBack);
        return new DateTimeOffset(monday.Year, monday.Month, monday.Day, 0, 0, 0, offset);
    }

    /// <summary>
    /// EET_US_DST: UTC+2 normally; UTC+3 from 2nd Sunday of March to 1st Sunday of November.
    /// Transition evaluated at the UTC instant (US rules).
    /// </summary>
    private static TimeSpan EetUsDstOffset(DateTimeOffset utc)
    {
        var year = utc.UtcDateTime.Year;
        var start = NthSunday(year, 3, 2); // 2nd Sunday March 00:00 UTC approximation for boundary
        var end = NthSunday(year, 11, 1);  // 1st Sunday November

        // DST active from start (inclusive) until end (exclusive) in UTC terms matching golden vectors
        if (utc.UtcDateTime >= start && utc.UtcDateTime < end)
            return TimeSpan.FromHours(3);
        return TimeSpan.FromHours(2);
    }

    private static DateTime NthSunday(int year, int month, int n)
    {
        var first = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        int daysUntilSunday = ((int)DayOfWeek.Sunday - (int)first.DayOfWeek + 7) % 7;
        return first.AddDays(daysUntilSunday + 7 * (n - 1));
    }
}
