using MyChart.Core.Candles;
using MyChart.Core.Models.Market;

namespace MyChart.Core.Scale;

/// <summary>
/// T2.01 TimeIndexMapper — INDEX-BASED X axis.
/// u = logical bar coordinate; bar i occupies [i-0.5, i+0.5]; centre at u=i.
/// Gaps take no index space.
/// </summary>
public sealed class TimeIndexMapper
{
    private readonly DateTimeOffset[] _opens;
    private readonly Timeframe _tf;
    private readonly SessionCalendar _calendar;

    public TimeIndexMapper(
        IReadOnlyList<DateTimeOffset> barOpens,
        Timeframe timeframe,
        SessionCalendar calendar)
    {
        _opens = barOpens?.ToArray() ?? Array.Empty<DateTimeOffset>();
        _tf = timeframe;
        _calendar = calendar;
    }

    public int N => _opens.Length;

    /// <summary>IndexOfTime(t) → u</summary>
    public double IndexOfTime(DateTimeOffset t)
    {
        int n = N;
        if (n == 0) return 0;

        // t < Open[0]
        if (t < _opens[0])
            return -BucketDistance(t, _opens[0]);

        // binary search last open <= t
        int lo = 0, hi = n - 1, i = 0;
        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            if (_opens[mid] <= t)
            {
                i = mid;
                lo = mid + 1;
            }
            else hi = mid - 1;
        }

        var open = _opens[i];
        var span = SpanAt(open);
        var barEnd = open + span;

        // inside bar [Open[i], Open[i]+Span)
        if (t < barEnd)
            return i + (t - open).TotalSeconds / span.TotalSeconds;

        // after last bar
        if (i == n - 1)
            return (n - 1) + BucketDistance(open, t);

        // gap after bar i: t >= Open[i]+Span and t < Open[i+1] → u = i + 1
        if (t < _opens[i + 1])
            return i + 1;

        // should not reach (binary search found last open <= t)
        return i + 1;
    }

    /// <summary>TimeAtIndex(u)</summary>
    public DateTimeOffset TimeAtIndex(double u)
    {
        int n = N;
        if (n == 0) return default;

        if (u >= 0 && u <= n - 1)
        {
            int i = (int)Math.Floor(u);
            double frac = u - i;
            var span = SpanAt(_opens[i]);
            return _opens[i] + TimeSpan.FromSeconds(frac * span.TotalSeconds);
        }

        if (u > n - 1)
        {
            double delta = u - (n - 1);
            int whole = (int)Math.Floor(delta);
            double frac = delta - whole;
            var baseOpen = AddBuckets(_opens[n - 1], whole);
            var span = SpanAt(baseOpen);
            return baseOpen + TimeSpan.FromSeconds(frac * span.TotalSeconds);
        }

        // u < 0
        {
            int whole = (int)Math.Floor(u); // negative or zero
            double frac = u - whole;
            var baseOpen = AddBuckets(_opens[0], whole);
            var span = SpanAt(baseOpen);
            return baseOpen + TimeSpan.FromSeconds(frac * span.TotalSeconds);
        }
    }

    /// <summary>SnapIndex(u) = floor(u + 0.5) (round half up toward +∞ for positive).</summary>
    public int SnapIndex(double u)
        => (int)Math.Floor(u + 0.5);

    /// <summary>
    /// BucketDistance(a,b): fixed TF (b-a)/Span; W1 (b-a)/7d; MN1 calendar months + fraction.
    /// </summary>
    public double BucketDistance(DateTimeOffset a, DateTimeOffset b)
    {
        if (b < a)
            return BucketDistance(b, a); // magnitude used by callers with sign outside

        if (_tf is Timeframe.W1)
            return (b - a).TotalDays / 7.0;

        if (_tf is Timeframe.MN1)
            return MonthDistance(a, b);

        var span = SpanAt(a);
        if (span.TotalSeconds <= 0) return 0;
        return (b - a).TotalSeconds / span.TotalSeconds;
    }

    public DateTimeOffset AddBuckets(DateTimeOffset start, int count)
    {
        if (count == 0) return start;
        var t = start;
        if (count > 0)
        {
            for (int i = 0; i < count; i++)
                t = TimeBuckets.Next(t, _tf, _calendar);
        }
        else
        {
            for (int i = 0; i < -count; i++)
                t = TimeBuckets.Prev(t, _tf, _calendar);
        }
        return t;
    }

    private TimeSpan SpanAt(DateTimeOffset open)
        => TimeBuckets.Span(open, _tf, _calendar);

    private static double MonthDistance(DateTimeOffset a, DateTimeOffset b)
    {
        // whole calendar months between + fraction of the ending month
        int months = (b.Year - a.Year) * 12 + (b.Month - a.Month);
        var afterWhole = a.AddMonths(months);
        if (afterWhole > b)
        {
            months--;
            afterWhole = a.AddMonths(months);
        }
        var nextMonth = afterWhole.AddMonths(1);
        double frac = (b - afterWhole).TotalSeconds / (nextMonth - afterWhole).TotalSeconds;
        return months + frac;
    }
}
