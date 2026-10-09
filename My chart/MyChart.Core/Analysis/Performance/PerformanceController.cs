using MyChart.Core.Models.Settings;

namespace MyChart.Core.Analysis.Performance;

/// <summary>
/// T7.03 — binds Settings performance.* keys; GPU acceleration stays NOT_IN_V1 (software only).
/// </summary>
public sealed class PerformanceController
{
    public FrameBudget FrameBudget { get; } = new();
    public LayerCache Layers { get; } = new();
    public DirtyRegionTracker Dirty { get; } = new();
    public PaintPathPool PathPool { get; } = new();
    public FpsMeter FpsMeter { get; } = new();

    public bool AntiAliasing { get; private set; } = true;

    /// <summary>performance.gpu.acceleration is NOT_IN_V1 — always false.</summary>
    public bool GpuAcceleration => false;

    public void ApplySettings(ChartSettingValues values)
    {
        ArgumentNullException.ThrowIfNull(values);
        FrameBudget.FpsLimit = values.FpsLimit > 0 ? values.FpsLimit : FrameBudget.DefaultFpsLimit;
        AntiAliasing = values.AntiAliasing;
    }

    public void OnViewportChanged()
    {
        Layers.InvalidateCachedLayers();
        Dirty.MarkFull();
    }

    public void OnOverlayChanged(int x, int y, int w, int h)
    {
        Dirty.Mark(x, y, w, h);
    }
}
