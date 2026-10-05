using MyChart.Core.Contracts.Services;

namespace MyChart.PluginHost.Guard;

/// <summary>
/// TRUST_AND_FAULTS: opens after 3 faults in 60s or 5 consecutive frames above 8ms.
/// </summary>
public sealed class CircuitBreaker
{
    public const int FaultThreshold = 3;
    public static readonly TimeSpan FaultWindow = TimeSpan.FromSeconds(60);
    public const int SlowFrameThreshold = 5;
    public const double SlowFrameMs = 8.0;

    private readonly IClock _clock;
    private readonly Queue<DateTimeOffset> _faults = new();
    private int _consecutiveSlow;

    public CircuitBreaker(IClock clock) => _clock = clock;

    public bool IsOpen { get; private set; }

    public void RecordFault()
    {
        var now = _clock.UtcNow;
        _faults.Enqueue(now);
        while (_faults.Count > 0 && now - _faults.Peek() > FaultWindow)
            _faults.Dequeue();
        if (_faults.Count >= FaultThreshold)
            IsOpen = true;
    }

    public void RecordFrame(double durationMs)
    {
        if (durationMs > SlowFrameMs)
            _consecutiveSlow++;
        else
            _consecutiveSlow = 0;

        if (_consecutiveSlow >= SlowFrameThreshold)
            IsOpen = true;
    }

    public void Reset()
    {
        IsOpen = false;
        _faults.Clear();
        _consecutiveSlow = 0;
    }
}
