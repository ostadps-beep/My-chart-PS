namespace MyChart.Core.Performance;

/// <summary>
/// T7.03 — minimum frame interval = 1000 / performance.fps.limit ms. Target 60 FPS (p95 &lt;= 16.7 ms).
/// </summary>
public sealed class FrameLimiter
{
    public const int DefaultFpsLimit = 60;
    public const double TargetFrameMs = 1000.0 / DefaultFpsLimit;

    private long _lastFrameTicks;
    private int _fpsLimit = DefaultFpsLimit;

    public int FpsLimit
    {
        get => _fpsLimit;
        set => _fpsLimit = Math.Clamp(value, 1, 240);
    }

    public double MinIntervalMs => 1000.0 / FpsLimit;

    /// <summary>True when enough time has elapsed since the last accepted frame.</summary>
    public bool ShouldRender(long nowTimestampTicks)
    {
        if (_lastFrameTicks == 0)
        {
            _lastFrameTicks = nowTimestampTicks;
            return true;
        }

        double elapsedMs = (nowTimestampTicks - _lastFrameTicks) * 1000.0 / TimeSpan.TicksPerSecond;
        if (elapsedMs + 1e-9 < MinIntervalMs)
            return false;

        _lastFrameTicks = nowTimestampTicks;
        return true;
    }

    public void Reset() => _lastFrameTicks = 0;
}
