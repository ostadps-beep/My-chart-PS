using MyChart.Core.Models.Market;

namespace MyChart.Core.Candles;

/// <summary>
/// T1.05 TickFilter — stateful per symbol. Rejects NaN/Inf, Bid&lt;=0, Ask&lt;Bid,
/// out-of-order timestamps, future timestamps, and spikes (with 3-tick level-shift exception).
/// </summary>
public sealed class TickFilter
{
    private DateTimeOffset? _lastAcceptedTs;
    private double? _prevBid;
    private int _acceptedCount;

    // Consecutive spike candidates for the 3-tick level-shift exception
    private readonly List<Tick> _spikeRun = new(3);

    public int RejectedCount { get; private set; }

    public bool TryAccept(Tick tick, DateTimeOffset nowUtc, out Tick accepted)
    {
        accepted = default;

        // NaN / Infinity / Bid <= 0 / Ask < Bid
        if (double.IsNaN(tick.Bid) || double.IsInfinity(tick.Bid)
            || double.IsNaN(tick.Ask) || double.IsInfinity(tick.Ask)
            || double.IsNaN(tick.Volume) || double.IsInfinity(tick.Volume)
            || tick.Bid <= 0
            || tick.Ask < tick.Bid)
        {
            RejectedCount++;
            return false;
        }

        // Out of order (equal timestamps allowed)
        if (_lastAcceptedTs.HasValue && tick.Timestamp < _lastAcceptedTs.Value)
        {
            RejectedCount++;
            return false;
        }

        // Future
        if (tick.Timestamp > nowUtc + TimeSpan.FromMinutes(5))
        {
            RejectedCount++;
            return false;
        }

        // Spike detection after at least 10 accepted ticks
        if (_acceptedCount >= 10 && _prevBid.HasValue && _prevBid.Value > 0)
        {
            double pct = Math.Abs(tick.Bid - _prevBid.Value) / _prevBid.Value * 100.0;
            if (pct > 5.0)
            {
                // Candidate for level-shift exception
                _spikeRun.Add(tick);

                if (_spikeRun.Count >= 3 && SpikesAgree(_spikeRun))
                {
                    // Accept the THIRD; first two stay rejected (already counted)
                    var third = _spikeRun[2];
                    _spikeRun.Clear();
                    Accept(third);
                    accepted = third;
                    return true;
                }

                // Not yet three agreeing — reject this one
                RejectedCount++;
                if (_spikeRun.Count > 3)
                    _spikeRun.RemoveAt(0);
                return false;
            }

            // Not a spike — clear any partial spike run
            _spikeRun.Clear();
        }

        Accept(tick);
        accepted = tick;
        return true;
    }

    private void Accept(Tick tick)
    {
        _lastAcceptedTs = tick.Timestamp;
        _prevBid = tick.Bid;
        _acceptedCount++;
    }

    private static bool SpikesAgree(List<Tick> run)
    {
        if (run.Count < 3) return false;
        // last three agree within 1 percent of each other
        var a = run[^3].Bid;
        var b = run[^2].Bid;
        var c = run[^1].Bid;
        return WithinOnePercent(a, b) && WithinOnePercent(b, c) && WithinOnePercent(a, c);
    }

    private static bool WithinOnePercent(double x, double y)
    {
        if (x == 0 && y == 0) return true;
        var basePrice = Math.Max(Math.Abs(x), Math.Abs(y));
        if (basePrice == 0) return false;
        return Math.Abs(x - y) / basePrice * 100.0 <= 1.0;
    }
}
