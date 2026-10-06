using MyChart.Core.Contracts.Services;

namespace MyChart.Core.Services;

/// <summary>
/// T5.05 — IClock for replay. Backtest advances only when Set/Advance is called;
/// MarketReplay scales wall elapsed by Speed.
/// </summary>
public sealed class ReplayClock : IClock
{
    private DateTimeOffset _utcNow;
    private DateTimeOffset _wallAnchor;
    private DateTimeOffset _simAnchor;
    private double _speed = 1.0;
    private bool _running;

    public ReplayClock(DateTimeOffset startUtc)
    {
        _utcNow = startUtc;
        _simAnchor = startUtc;
        _wallAnchor = DateTimeOffset.UtcNow;
    }

    public DateTimeOffset UtcNow
    {
        get
        {
            if (_running && _speed > 0)
            {
                var wallElapsed = DateTimeOffset.UtcNow - _wallAnchor;
                _utcNow = _simAnchor + TimeSpan.FromTicks((long)(wallElapsed.Ticks * _speed));
            }
            return _utcNow;
        }
    }

    public double Speed
    {
        get => _speed;
        set => _speed = value <= 0 ? 0 : value;
    }

    public bool IsRunning => _running;

    public void Start()
    {
        _wallAnchor = DateTimeOffset.UtcNow;
        _simAnchor = _utcNow;
        _running = true;
    }

    public void Pause()
    {
        _ = UtcNow;
        _running = false;
    }

    /// <summary>Backtest: jump simulation time to an absolute UTC instant.</summary>
    public void Set(DateTimeOffset utc)
    {
        _utcNow = utc;
        _simAnchor = utc;
        _wallAnchor = DateTimeOffset.UtcNow;
    }

    /// <summary>Advance simulation by a fixed delta (Backtest step).</summary>
    public void Advance(TimeSpan delta) => Set(_utcNow + delta);
}
