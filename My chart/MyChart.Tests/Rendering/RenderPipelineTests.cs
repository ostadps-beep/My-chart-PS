using MyChart.Core.Contracts.Rendering;
using MyChart.Core.Models.Rendering;
using MyChart.Core.Rendering;
using MyChart.Tests.Plugins;
using Xunit;

namespace MyChart.Tests.Rendering;

public class RenderPipelineTests
{
    private static void RegisterAllLayers(RenderPipeline pipeline, RecordingRenderContext ctx)
    {
        for (var i = 1; i <= 10; i++)
        {
            var layer = (RenderLayer)i;
            pipeline.SetLayer(layer, _ => ctx.NoteLayer(layer));
        }
    }

    [Fact]
    public void Initial_state_is_dirty()
    {
        var pipeline = new RenderPipeline();
        Assert.True(pipeline.IsBaseDirty);
        Assert.True(pipeline.IsOverlayDirty);
        Assert.True(pipeline.IsDirty);
    }

    [Fact]
    public void Render_with_no_layers_clears_dirty_and_returns_true()
    {
        var pipeline = new RenderPipeline();
        var ctx = new RecordingRenderContext();
        Assert.True(pipeline.Render(ctx));
        Assert.False(pipeline.IsDirty);
        Assert.Empty(ctx.LayerCalls);
    }

    [Fact]
    public void Full_render_issues_layers_in_order_1_to_10()
    {
        var pipeline = new RenderPipeline();
        var ctx = new RecordingRenderContext();
        RegisterAllLayers(pipeline, ctx);

        pipeline.Render(ctx);

        Assert.Equal(10, ctx.LayerCalls.Count);
        for (var i = 0; i < 10; i++)
            Assert.Equal((RenderLayer)(i + 1), ctx.LayerCalls[i]);
        Assert.False(pipeline.IsDirty);
    }

    [Fact]
    public void Second_render_with_no_invalidate_issues_nothing()
    {
        var pipeline = new RenderPipeline();
        var ctx = new RecordingRenderContext();
        pipeline.SetLayer(RenderLayer.Background, c => c.Clear(RgbaColor.FromRgb(0, 0, 0)));
        pipeline.Render(ctx);
        ctx.LayerCalls.Clear();
        ctx.Operations.Clear();

        Assert.False(pipeline.Render(ctx));
        Assert.Empty(ctx.LayerCalls);
        Assert.Empty(ctx.Operations);
    }

    [Fact]
    public void Overlay_invalidate_reissues_only_layers_6_to_10()
    {
        var pipeline = new RenderPipeline();
        var ctx = new RecordingRenderContext();
        RegisterAllLayers(pipeline, ctx);

        pipeline.Render(ctx);
        ctx.LayerCalls.Clear();

        pipeline.Invalidate(InvalidateReason.CrosshairMoved);
        Assert.False(pipeline.IsBaseDirty);
        Assert.True(pipeline.IsOverlayDirty);

        pipeline.Render(ctx);

        Assert.Equal(5, ctx.LayerCalls.Count);
        Assert.Equal(RenderLayer.Selection, ctx.LayerCalls[0]);
        Assert.Equal(RenderLayer.Crosshair, ctx.LayerCalls[1]);
        Assert.Equal(RenderLayer.Hud, ctx.LayerCalls[2]);
        Assert.Equal(RenderLayer.Tooltip, ctx.LayerCalls[3]);
        Assert.Equal(RenderLayer.Overlay, ctx.LayerCalls[4]);
        Assert.False(pipeline.IsDirty);
    }

    [Theory]
    [InlineData(InvalidateReason.SizeChanged)]
    [InlineData(InvalidateReason.ThemeChanged)]
    [InlineData(InvalidateReason.RangeChanged)]
    [InlineData(InvalidateReason.DataChanged)]
    [InlineData(InvalidateReason.SeriesChanged)]
    public void Base_invalidate_rebuilds_all_layers(InvalidateReason reason)
    {
        var pipeline = new RenderPipeline();
        var ctx = new RecordingRenderContext();
        RegisterAllLayers(pipeline, ctx);

        pipeline.Render(ctx);
        ctx.LayerCalls.Clear();

        pipeline.Invalidate(reason);
        Assert.True(pipeline.IsBaseDirty);
        Assert.True(pipeline.IsOverlayDirty);

        pipeline.Render(ctx);
        Assert.Equal(10, ctx.LayerCalls.Count);
        Assert.Equal(RenderLayer.Background, ctx.LayerCalls[0]);
        Assert.Equal(RenderLayer.Overlay, ctx.LayerCalls[9]);
    }

    [Theory]
    [InlineData(InvalidateReason.CrosshairMoved)]
    [InlineData(InvalidateReason.HudChanged)]
    [InlineData(InvalidateReason.SelectionChanged)]
    [InlineData(InvalidateReason.OverlayChanged)]
    public void Overlay_reasons_do_not_mark_base_dirty(InvalidateReason reason)
    {
        var pipeline = new RenderPipeline();
        pipeline.Render(new RecordingRenderContext());
        pipeline.Invalidate(reason);
        Assert.False(pipeline.IsBaseDirty);
        Assert.True(pipeline.IsOverlayDirty);
    }

    [Fact]
    public void Layer_paint_actions_are_invoked_with_context()
    {
        var pipeline = new RenderPipeline();
        var ctx = new RecordingRenderContext();
        var painted = false;
        pipeline.SetLayer(RenderLayer.Candle, c =>
        {
            painted = true;
            c.FillRect(0, 0, 10, 10, RgbaColor.FromRgb(255, 0, 0));
        });

        pipeline.Render(ctx);

        Assert.True(painted);
        Assert.Contains("FillRect", ctx.Operations);
    }

    [Fact]
    public void SetLayer_rejects_null_renderer()
    {
        var pipeline = new RenderPipeline();
        Assert.Throws<ArgumentNullException>(() => pipeline.SetLayer((ILayerRenderer)null!));
    }
}
