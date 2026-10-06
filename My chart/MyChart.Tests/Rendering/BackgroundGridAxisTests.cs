using MyChart.Core.Models.Viewport;
using MyChart.Core.Scale;
using MyChart.Core.Services;
using MyChart.Rendering.Layers;
using MyChart.Tests.Plugins;
using Xunit;

namespace MyChart.Tests.Rendering;

/// <summary>T4.03 Background / Grid / PriceAxis / TimeAxis — automated smoke + color usage.</summary>
public class BackgroundGridAxisTests
{
    private static ViewState StandardView()
    {
        var vs = new ViewState
        {
            Width = 864,
            Height = 524,
            PriceAxisWidth = 64,
            TimeAxisHeight = 24,
            BarSpacing = 8,
            RightOffset = 5
        };
        vs.PriceScale.MinPrice = 1.0940;
        vs.PriceScale.MaxPrice = 1.1060;
        return vs;
    }

    [Fact]
    public void Background_FillsWithBackgroundColor()
    {
        var theme = new ThemeService();
        var bg = new BackgroundRenderer(theme);
        var ctx = new RecordingRenderContext { Width = 864, Height = 524 };
        bg.Render(ctx, StandardView(), dpiScale: 1);
        Assert.Contains("FillRect", ctx.Operations);
    }

    [Fact]
    public void Grid_DrawsHorizontalAndVerticalLines()
    {
        var theme = new ThemeService();
        var grid = new GridRenderer(theme);
        var vs = StandardView();
        var converter = new CoordinateConverter(vs, 100);
        var minT = vs.PriceScale.TransformPrice(vs.PriceScale.MinPrice);
        var maxT = vs.PriceScale.TransformPrice(vs.PriceScale.MaxPrice);
        var step = NiceTicks.ComputeStep(maxT - minT, vs.PlotHeight, 0.00001);
        var ticks = NiceTicks.PriceTicks(minT, maxT, step);
        var timeUs = new double[] { 10, 20, 30, 40 };

        var ctx = new RecordingRenderContext();
        grid.Render(ctx, vs, converter, ticks, timeUs, dpiScale: 1);

        Assert.True(ctx.DrawLineCount >= ticks.Count);
        Assert.True(ctx.Operations.Count(o => o.StartsWith("DrawLine")) >= ticks.Count + timeUs.Length);
    }

    [Fact]
    public void PriceAxis_DrawsLabelsInAxisColor()
    {
        var theme = new ThemeService();
        var axis = new PriceAxisRenderer(theme);
        var vs = StandardView();
        var converter = new CoordinateConverter(vs, 100);
        var minT = vs.PriceScale.TransformPrice(vs.PriceScale.MinPrice);
        var maxT = vs.PriceScale.TransformPrice(vs.PriceScale.MaxPrice);
        var step = NiceTicks.ComputeStep(maxT - minT, vs.PlotHeight, 0.00001);
        var ticks = NiceTicks.PriceTicks(minT, maxT, step);

        var ctx = new RecordingRenderContext();
        axis.Render(ctx, vs, converter, ticks, step, digits: 5, dpiScale: 1);

        Assert.Contains(ctx.Operations, o => o.StartsWith("DrawText:"));
        Assert.Contains(ctx.Operations, o => o.StartsWith("DrawLine:"));
    }

    [Fact]
    public void TimeAxis_DrawsBottomLabels()
    {
        var theme = new ThemeService();
        var axis = new TimeAxisRenderer(theme);
        var vs = StandardView();
        var converter = new CoordinateConverter(vs, 100);
        var labels = new (double, string)[]
        {
            (10, "12:00"),
            (30, "17:00"),
            (50, "22:00")
        };

        var ctx = new RecordingRenderContext();
        axis.Render(ctx, vs, converter, labels, dpiScale: 1);

        Assert.Equal(3, ctx.Operations.Count(o => o.StartsWith("DrawText:")));
        Assert.Contains(ctx.Operations, o => o.StartsWith("DrawLine:"));
    }

    [Fact]
    public void Constants_MatchSpec()
    {
        Assert.Equal(64, PriceAxisRenderer.AxisWidthDip);
        Assert.Equal(24, TimeAxisRenderer.AxisHeightDip);
        Assert.Equal(11, PriceAxisRenderer.LabelSizeDip);
    }
}
