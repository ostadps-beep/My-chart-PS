using MyChart.Core.Candles;
using MyChart.Core.Models.Market;
using MyChart.Core.Models.Viewport;

namespace MyChart.Core.Scale;

/// <summary>T2.10 CurrentPrice and DualMarker pure values.</summary>
public readonly record struct CurrentPriceInfo(
    double Price,
    bool IsBull,
    double MarkerY,
    bool IsClamped,
    string? CountdownText);

public static class CurrentPriceValues
{
    public static double ResolveCurrentPrice(double? lastTickBid, Candle? lastCandle)
    {
        if (lastTickBid.HasValue) return lastTickBid.Value;
        if (lastCandle.HasValue) return lastCandle.Value.Close;
        return 0;
    }

    public static bool IsBullDirection(double currentPrice, double formingOpen)
        => currentPrice >= formingOpen;

    public static (double Y, bool Clamped) ClampMarkerY(double priceY, ViewState vs)
    {
        double top = vs.PlotTop;
        double bottom = vs.PlotTop + vs.PlotHeight;
        if (priceY < top) return (top, true);
        if (priceY > bottom) return (bottom, true);
        return (priceY, false);
    }

    /// <summary>EMA alpha 0.2 of (tick.Timestamp - localReceive).</summary>
    public static TimeSpan UpdateClockSkew(TimeSpan previousSkew, DateTimeOffset tickTs, DateTimeOffset localReceive)
    {
        var sample = tickTs - localReceive;
        const double alpha = 0.2;
        return TimeSpan.FromTicks((long)(previousSkew.Ticks * (1 - alpha) + sample.Ticks * alpha));
    }

    public static TimeSpan Countdown(
        DateTimeOffset formingOpen,
        Timeframe tf,
        SessionCalendar calendar,
        DateTimeOffset utcNow,
        TimeSpan clockSkew)
    {
        var next = TimeBuckets.Next(formingOpen, tf, calendar);
        var serverNow = utcNow + clockSkew;
        var remaining = next - serverNow;
        return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
    }

    public static string FormatCountdown(TimeSpan remaining)
    {
        if (remaining < TimeSpan.FromHours(1))
            return $"{(int)remaining.TotalMinutes}:{remaining.Seconds:D2}";
        if (remaining < TimeSpan.FromHours(24))
            return $"{(int)remaining.TotalHours}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
        return $"{(int)remaining.TotalDays}d {remaining.Hours:D2}:{remaining.Minutes:D2}";
    }
}
