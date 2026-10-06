using MyChart.Core.Models.Market;
using MyChart.Core.Services;
using MyChart.Data.Aggregation;

namespace MyChart.Data.Replay;

/// <summary>
/// T5.05 ReplayEngine — drives the same AggregationEngine.OnTick path via ReplayClock.
/// Sources: in-memory ticks (CSV / DB / Provider load into this list).
/// </summary>
public sealed class ReplayEngine
{
    public static readonly double[] AllowedSpeeds = { 1, 2, 5, 10, 50 };

    private readonly AggregationEngine _aggregation;
    private readonly ReplayClock _clock;
    private readonly string _symbol;
    private IReadOnlyList<Tick> _ticks = Array.Empty<Tick>();
    private int _index;
    private double _speed = 1;

    public ReplayEngine(string symbol, AggregationEngine aggregation, ReplayClock clock)
    {
        _symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
        _aggregation = aggregation ?? throw new ArgumentNullException(nameof(aggregation));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public ReplayMode Mode { get; private set; } = ReplayMode.Backtest;
    public ReplayClock Clock => _clock;
    public int CursorIndex => _index;
    public int TickCount => _ticks.Count;
    public bool IsFinished => _index >= _ticks.Count;
    public double Speed => _speed;

    /// <summary>Bars at or after this UTC are considered "after cursor" for Training hide rules.</summary>
    public DateTimeOffset CursorTime => _clock.UtcNow;

    public void LoadTicks(IReadOnlyList<Tick> ticks)
    {
        _ticks = ticks.OrderBy(t => t.Timestamp).ToList();
        _index = 0;
        if (_ticks.Count > 0)
            _clock.Set(_ticks[0].Timestamp);
    }

    public void LoadFromCsv(string path) => LoadTicks(TickCsvLoader.Load(path));

    public void SetMode(ReplayMode mode)
    {
        Mode = mode;
        if (mode == ReplayMode.Backtest)
        {
            _clock.Pause();
            _clock.Speed = 0;
        }
        else
        {
            if (!AllowedSpeeds.Contains(_speed))
                _speed = 1;
            _clock.Speed = _speed;
        }
    }

    public void SetSpeed(double speed)
    {
        if (!AllowedSpeeds.Contains(speed))
            throw new ArgumentOutOfRangeException(nameof(speed), "Speed must be one of 1,2,5,10,50.");
        _speed = speed;
        if (Mode != ReplayMode.Backtest)
            _clock.Speed = speed;
    }

    /// <summary>
    /// Backtest: process all remaining ticks as fast as possible (no wall wait).
    /// Returns number of ticks applied.
    /// </summary>
    public int RunBacktestToEnd()
    {
        SetMode(ReplayMode.Backtest);
        int applied = 0;
        while (_index < _ticks.Count)
        {
            ApplyOne();
            applied++;
        }
        return applied;
    }

    /// <summary>Training / stepped: apply the next tick only.</summary>
    public bool StepForward()
    {
        if (_index >= _ticks.Count)
            return false;
        ApplyOne();
        return true;
    }

    /// <summary>
    /// MarketReplay: apply every tick whose timestamp is &lt;= clock.UtcNow.
    /// Caller advances wall time externally (or Start clock).
    /// </summary>
    public int PumpDueTicks()
    {
        int applied = 0;
        var now = _clock.UtcNow;
        while (_index < _ticks.Count && _ticks[_index].Timestamp <= now)
        {
            ApplyOne();
            applied++;
        }
        return applied;
    }

    /// <summary>
    /// Training: series should hide closed bars with open time &gt; CursorTime.
    /// Helper for views — pure filter.
    /// </summary>
    public static IReadOnlyList<Candle> VisibleBars(IReadOnlyList<Candle> closed, DateTimeOffset cursor)
        => closed.Where(c => c.Timestamp <= cursor).ToList();

    private void ApplyOne()
    {
        var tick = _ticks[_index++];
        _clock.Set(tick.Timestamp);
        _aggregation.OnTick(_symbol, tick);
    }
}
