using MyChart.Core.Models.Settings;

namespace MyChart.Core.Performance;

/// <summary>
/// T7.03 PerformanceTuning — FPS limit, anti-aliasing, and budget helpers from Settings keys.
/// GPU acceleration stays NOT_IN_V1 (software rendering).
/// </summary>
public sealed class PerformanceController
{
    public FrameLimiter FrameLimiter { get; } = new();
    public bool AntiAliasing { get; private set; } = true;
    public bool GpuAcceleration => false; // NOT_IN_V1

    /// <summary>p95 frame time budget in ms while panning 2000 visible bars.</summary>
    public const double P95FrameBudgetMs = 16.7;

    public void ApplySettings(ChartSettingValues values)
    {
        ArgumentNullException.ThrowIfNull(values);
        FrameLimiter.FpsLimit = values.FpsLimit <= 0 ? FrameLimiter.DefaultFpsLimit : values.FpsLimit;
        AntiAliasing = values.AntiAliasing;
    }

    public bool WithinBudget(double frameMs) => frameMs <= P95FrameBudgetMs;
}
