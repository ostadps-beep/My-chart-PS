using MyChart.Core.Models.Market;

namespace MyChart.Core.Candles;

/// <summary>
/// T1.06 FormingCandle — mutable forming bar logic (L2 pure helpers).
/// Open = first tick Bid of the bucket (not previous Close).
/// CANDLE_PRICE_BASIS = Bid.
/// </summary>
public struct FormingCandle
{
    public DateTimeOffset Timestamp;
    public double Open;
    public double High;
    public double Low;
    public double Close;
    public double Volume;
    public bool IsEmpty;

    public static FormingCandle OpenNew(DateTimeOffset bucketStart, Tick tick)
    {
        double vol = tick.Volume > 0 ? tick.Volume : 1;
        return new FormingCandle
        {
            Timestamp = bucketStart,
            Open = tick.Bid,
            High = tick.Bid,
            Low = tick.Bid,
            Close = tick.Bid,
            Volume = vol,
            IsEmpty = false
        };
    }

    public void Apply(Tick tick)
    {
        High = Math.Max(High, tick.Bid);
        Low = Math.Min(Low, tick.Bid);
        Close = tick.Bid;
        Volume += tick.Volume > 0 ? tick.Volume : 1;
    }

    public Candle ToCandle() => new(Timestamp, Open, High, Low, Close, Volume);

    public static FormingCandle Empty => new() { IsEmpty = true };
}
