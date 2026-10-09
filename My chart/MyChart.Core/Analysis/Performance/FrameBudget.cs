namespace MyChart.Core.Analysis.Performance;

/// <summary>
/// T7.03 — FPS limit from settings (performance.fps.limit).
/// TARGET: 60 FPS default → frame budget 16.7 ms; p95 pan with 2000 bars measured by host.
/// </summary>
public sealed class FrameBudget
{
    public const int DefaultFpsLimit = 60;
    public const double TargetFrameMsAt60 = 1000.0 / 60.0; // 16.666…

    private int _fpsLimit = DefaultFpsLimit;
    private DateTimeOffset _lastPresent = DateTimeOffset.MinValue;

    public int FpsLimit
    {
        get => _fpsLimit;
        set => _fpsLimit = Math.Clamp(value, 1, 240);
    }

    public double FrameBudgetMs => 1000.0 / _fpsLimit;

    /// <summary>True when enough time has elapsed since last present for the configured FPS limit.</summary>
    public bool ShouldPresent(DateTimeOffset now)
    {
        if (_lastPresent == DateTimeOffset.MinValue)
        {
            _lastPresent = now;
            return true;
        }

        var elapsed = (now - _lastPresent).TotalMilliseconds;
        if (elapsed + 0.01 >= FrameBudgetMs)
        {
            _lastPresent = now;
            return true;
        }

        return false;
    }

    public void Reset() => _lastPresent = DateTimeOffset.MinValue;
}
