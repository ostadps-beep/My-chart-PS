using MyChart.Core.Models.Market;

namespace MyChart.Core.Candles;

/// <summary>
/// T1.06 — apply ticks to a single timeframe forming candle.
/// Returns closed candles emitted; updates forming in place.
/// Late ticks (Floor &lt; forming.Timestamp) are discarded and counted.
/// </summary>
public sealed class TickAggregator
{
    private readonly Timeframe _tf;
    private readonly SessionCalendar _calendar;
    private FormingCandle _forming = FormingCandle.Empty;

    public TickAggregator(Timeframe tf, SessionCalendar calendar)
    {
        _tf = tf;
        _calendar = calendar;
    }

    public FormingCandle Forming => _forming;
    public int LateTicks { get; private set; }
    public IReadOnlyList<Candle> ClosedCandles => _closed;
    private readonly List<Candle> _closed = new();

    /// <summary>
    /// Apply one tick. Returns true if a candle was closed.
    /// </summary>
    public bool Apply(Tick tick, out Candle? closed)
    {
        closed = null;
        var bucket = TimeBuckets.Floor(tick.Timestamp, _tf, _calendar);

        if (_forming.IsEmpty)
        {
            _forming = FormingCandle.OpenNew(bucket, tick);
            return false;
        }

        if (bucket > _forming.Timestamp)
        {
            closed = _forming.ToCandle();
            _closed.Add(closed.Value);
            _forming = FormingCandle.OpenNew(bucket, tick);
            return true;
        }

        if (bucket < _forming.Timestamp)
        {
            LateTicks++;
            return false;
        }

        // same bucket
        _forming.Apply(tick);
        return false;
    }
}
