using MyChart.Core.Models.Market;

namespace MyChart.Core.Candles;

/// <summary>
/// T1.06 Fold — Timestamp = bucket start; Open = first.Open; High = max; Low = min; Close = last.Close; Volume = sum.
/// </summary>
public static class CandleFold
{
    public static Candle Fold(IReadOnlyList<Candle> sources, DateTimeOffset bucketStart)
    {
        if (sources == null || sources.Count == 0)
            throw new ArgumentException("Fold requires at least one source candle.", nameof(sources));

        double open = sources[0].Open;
        double high = sources[0].High;
        double low = sources[0].Low;
        double close = sources[^1].Close;
        double volume = 0;

        for (int i = 0; i < sources.Count; i++)
        {
            var c = sources[i];
            if (c.High > high) high = c.High;
            if (c.Low < low) low = c.Low;
            volume += c.Volume;
        }

        return new Candle(bucketStart, open, high, low, close, volume);
    }

    /// <summary>
    /// Aggregate source candles into target timeframe buckets (exact multiple span + nested boundaries).
    /// </summary>
    public static List<Candle> Aggregate(
        IReadOnlyList<Candle> sources,
        Timeframe sourceTf,
        Timeframe targetTf,
        SessionCalendar calendar)
    {
        if (sources.Count == 0) return new List<Candle>();

        var result = new List<Candle>();
        var bucket = new List<Candle>();
        DateTimeOffset? currentBucket = null;

        foreach (var c in sources.OrderBy(x => x.Timestamp))
        {
            var b = TimeBuckets.Floor(c.Timestamp, targetTf, calendar);
            if (currentBucket == null)
            {
                currentBucket = b;
                bucket.Add(c);
            }
            else if (b == currentBucket.Value)
            {
                bucket.Add(c);
            }
            else
            {
                result.Add(Fold(bucket, currentBucket.Value));
                bucket.Clear();
                currentBucket = b;
                bucket.Add(c);
            }
        }

        if (bucket.Count > 0 && currentBucket.HasValue)
            result.Add(Fold(bucket, currentBucket.Value));

        return result;
    }
}
