using MyChart.Core.Models.Market;
using MyChart.Core.Models.Viewport;
using MyChart.Core.Scale;
using MyChart.Core.Services;
using MyChart.Rendering.Layers;
using MyChart.Tests.Plugins;
using Xunit;

namespace MyChart.Tests.Rendering;

/// <summary>T4.04 CandleRenderer VERIFY — FillRect from geometry; theme colors.</summary>
public class CandleRendererTests
{
    private static CoordinateConverter Cc(double barSpacing = 8)
    {
        var vs = new ViewState
        {
            Width = 864,
            Height = 400,
            PriceAxisWidth = 64,
            TimeAxisHeight = 24,
            BarSpacing = barSpacing,
            RightOffset = 5
        };
        vs.PriceScale.MinPrice = 1.09;
        vs.PriceScale.MaxPrice = 1.12;
        return new CoordinateConverter(vs, 20);
    }

    [Fact]
    public void SolidCandle_DrawsWickAndBody_FillRects()
    {
        var theme = new ThemeService();
        var renderer = new CandleRenderer(theme) { Style = CandleRenderStyle.Candles };
        var candle = new Candle(DateTimeOffset.UtcNow, 1.10, 1.11, 1.09, 1.105, 10);
        var g = CandleGeometryCalculator.Compute(candle, 5, Cc(), dpiScale: 1);
        var ctx = new RecordingRenderContext();
        renderer.PaintGeom(ctx, g);
        Assert.True(ctx.Operations.Count(o => o == "FillRect") >= 2);
    }

    [Fact]
    public void Highlight_AddsOutlineRects()
    {
        var theme = new ThemeService();
        var renderer = new CandleRenderer(theme);
        var candle = new Candle(DateTimeOffset.UtcNow, 1.10, 1.11, 1.09, 1.105, 10);
        var g = CandleGeometryCalculator.Compute(candle, 5, Cc(), dpiScale: 1);
        var ctx = new RecordingRenderContext();
        renderer.PaintGeom(ctx, g, highlight: true);
        Assert.True(ctx.Operations.Count(o => o == "FillRect") >= 6);
    }

    [Fact]
    public void Ohlc_DrawsVerticalAndTicks()
    {
        var theme = new ThemeService();
        var renderer = new CandleRenderer(theme) { Style = CandleRenderStyle.Ohlc };
        var candle = new Candle(DateTimeOffset.UtcNow, 1.10, 1.11, 1.09, 1.105, 10);
        var g = CandleGeometryCalculator.Compute(candle, 5, Cc(), dpiScale: 1);
        var ctx = new RecordingRenderContext();
        renderer.PaintGeom(ctx, g);
        Assert.True(ctx.Operations.Count(o => o == "FillRect") >= 3);
    }

    [Fact]
    public void Batch_Render_UsesGeometryForEachBar()
    {
        var theme = new ThemeService();
        var renderer = new CandleRenderer(theme);
        var candles = new List<Candle>
        {
            new(DateTimeOffset.UtcNow, 1.100, 1.105, 1.098, 1.103, 1),
            new(DateTimeOffset.UtcNow, 1.103, 1.108, 1.101, 1.102, 1),
            new(DateTimeOffset.UtcNow, 1.102, 1.104, 1.099, 1.100, 1),
        };
        var ctx = new RecordingRenderContext();
        renderer.Render(ctx, candles, firstIndex: 0, Cc(), dpiScale: 1);
        Assert.True(ctx.Operations.Count(o => o == "FillRect") >= 6);
    }

    [Fact]
    public void HollowBull_DrawsBorderEdges()
    {
        var theme = new ThemeService();
        var renderer = new CandleRenderer(theme) { Style = CandleRenderStyle.HollowCandles };
        var candle = new Candle(DateTimeOffset.UtcNow, 1.10, 1.11, 1.09, 1.105, 10);
        var g = CandleGeometryCalculator.Compute(candle, 5, Cc(barSpacing: 10), dpiScale: 1);
        Assert.True(g.IsBull);
        var ctx = new RecordingRenderContext();
        renderer.PaintGeom(ctx, g);
        Assert.True(ctx.Operations.Count(o => o == "FillRect") >= 1);
    }
}
