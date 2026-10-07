using MyChart.Core.Models.Indicators;
using MyChart.Core.Models.Viewport;
using MyChart.Core.Scale;
using MyChart.Core.Services;
using MyChart.Rendering.Layers;
using MyChart.Tests.Plugins;
using Xunit;

namespace MyChart.Tests.Rendering;

/// <summary>T6.06 Indicator layer — NaN breaks polyline; palette colors.</summary>
public class IndicatorRendererTests
{
    private static CoordinateConverter Cc()
    {
        var vs = new ViewState
        {
            Width = 800,
            Height = 400,
            PriceAxisWidth = 64,
            TimeAxisHeight = 24,
            BarSpacing = 8,
            RightOffset = 5
        };
        vs.PriceScale.MinPrice = 1.09;
        vs.PriceScale.MaxPrice = 1.12;
        return new CoordinateConverter(vs, 50);
    }

    [Fact]
    public void ContinuousSeries_DrawsOnePath()
    {
        var renderer = new IndicatorRenderer(new ThemeService());
        var values = new double[] { 1.10, 1.101, 1.102, 1.103, 1.104 };
        var output = new IndicatorOutput("SMA", values);
        var ctx = new RecordingRenderContext();
        renderer.Render(ctx, Cc(), new[] { output }, firstIndex: 0, dpiScale: 1);
        Assert.True(ctx.DrawPathCount >= 1);
    }

    [Fact]
    public void NaN_BreaksIntoMultiplePaths()
    {
        var renderer = new IndicatorRenderer(new ThemeService());
        // two segments separated by NaN
        var values = new double[] { 1.10, 1.101, double.NaN, 1.103, 1.104 };
        var output = new IndicatorOutput("SMA", values);
        var ctx = new RecordingRenderContext();
        renderer.Render(ctx, Cc(), new[] { output }, firstIndex: 0, dpiScale: 1);
        Assert.Equal(2, ctx.DrawPathCount);
    }

    [Fact]
    public void AllNaN_DrawsNothing()
    {
        var renderer = new IndicatorRenderer(new ThemeService());
        var values = new double[] { double.NaN, double.NaN };
        var output = new IndicatorOutput("SMA", values);
        var ctx = new RecordingRenderContext();
        renderer.Render(ctx, Cc(), new[] { output }, firstIndex: 0, dpiScale: 1);
        Assert.Equal(0, ctx.DrawPathCount);
    }

    [Fact]
    public void MultipleOutputs_DrawMultiplePaths()
    {
        var renderer = new IndicatorRenderer(new ThemeService());
        var a = new IndicatorOutput("A", new double[] { 1.10, 1.11, 1.12 });
        var b = new IndicatorOutput("B", new double[] { 1.09, 1.095, 1.10 });
        var ctx = new RecordingRenderContext();
        renderer.Render(ctx, Cc(), new[] { a, b }, firstIndex: 0, dpiScale: 1);
        Assert.Equal(2, ctx.DrawPathCount);
    }
}
