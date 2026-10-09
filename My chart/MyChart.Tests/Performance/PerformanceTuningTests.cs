using MyChart.Core.Analysis.Performance;
using MyChart.Core.Models.Settings;
using Xunit;

namespace MyChart.Tests.Performance;

/// <summary>T7.03 PerformanceTuning — automated contracts.</summary>
public class PerformanceTuningTests
{
    [Fact]
    public void FrameBudget_60Fps_IsAbout_16_7ms()
    {
        var b = new FrameBudget { FpsLimit = 60 };
        Assert.InRange(b.FrameBudgetMs, 16.6, 16.7);
    }

    [Fact]
    public void FrameBudget_ShouldPresent_RespectsLimit()
    {
        var b = new FrameBudget { FpsLimit = 10 }; // 100 ms
        var t0 = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        Assert.True(b.ShouldPresent(t0));
        Assert.False(b.ShouldPresent(t0.AddMilliseconds(50)));
        Assert.True(b.ShouldPresent(t0.AddMilliseconds(100)));
    }

    [Fact]
    public void LayerCache_Caches_1_to_5_Only()
    {
        var c = new LayerCache();
        Assert.True(c.IsCachedLayer(1));
        Assert.True(c.IsCachedLayer(5));
        Assert.False(c.IsCachedLayer(6));
        Assert.True(c.IsDirtyLayer(6));
        Assert.True(c.IsDirtyLayer(10));

        c.Set(3, "grid");
        Assert.True(c.TryGet(3, out var p));
        Assert.Equal("grid", p);
        c.Invalidate(3);
        Assert.False(c.TryGet(3, out _));
    }

    [Fact]
    public void DirtyRegion_Unions_Rects()
    {
        var d = new DirtyRegionTracker();
        d.Mark(10, 10, 20, 20);
        d.Mark(25, 25, 20, 20);
        Assert.True(d.HasDirty);
        var r = d.Region;
        Assert.Equal(10, r.X);
        Assert.Equal(10, r.Y);
        Assert.Equal(35, r.W);
        Assert.Equal(35, r.H);
        d.Clear();
        Assert.False(d.HasDirty);
    }

    [Fact]
    public void PaintPathPool_Reuses_Buffers()
    {
        var pool = new PaintPathPool();
        var a = pool.Rent();
        a.Add(1f);
        pool.Return(a);
        var b = pool.Rent();
        Assert.Empty(b);
        Assert.Same(a, b);
    }

    [Fact]
    public void NavigatorColumns_MinMax_PerPixel()
    {
        var highs = new double[] { 2, 4, 6, 8 };
        var lows = new double[] { 1, 3, 5, 7 };
        var cols = NavigatorColumnAggregator.Aggregate(highs, lows, pixelWidth: 2);
        Assert.Equal(2, cols.Length);
        Assert.Equal(1, cols[0].Min);
        Assert.Equal(4, cols[0].Max);
        Assert.Equal(5, cols[1].Min);
        Assert.Equal(8, cols[1].Max);
    }

    [Fact]
    public void PerformanceController_Reads_Settings_Keys()
    {
        var ctl = new PerformanceController();
        var v = new ChartSettingValues { FpsLimit = 30, AntiAliasing = false };
        ctl.ApplySettings(v);
        Assert.Equal(30, ctl.FrameBudget.FpsLimit);
        Assert.False(ctl.AntiAliasing);
        Assert.False(ctl.GpuAcceleration); // NOT_IN_V1
    }

    [Fact]
    public void ViewportChange_Invalidates_Cache_And_Marks_Full_Dirty()
    {
        var ctl = new PerformanceController();
        ctl.Layers.Set(1, "bg");
        ctl.OnViewportChanged();
        Assert.False(ctl.Layers.TryGet(1, out _));
        Assert.True(ctl.Dirty.IsFullSurface);
    }
}
