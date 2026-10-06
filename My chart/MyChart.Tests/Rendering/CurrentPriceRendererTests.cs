using MyChart.Core.Models.Viewport;
using MyChart.Core.Scale;
using MyChart.Core.Services;
using MyChart.Rendering.Layers;
using MyChart.Tests.Plugins;
using Xunit;

namespace MyChart.Tests.Rendering;

/// <summary>T4.05 CurrentPrice line + dual markers (market + analysis).</summary>
public class CurrentPriceRendererTests
{
    private static (ViewState vs, CoordinateConverter cc) Setup()
    {
        var vs = new ViewState
        {
            Width = 864,
            Height = 500,
            PriceAxisWidth = 64,
            TimeAxisHeight = 24,
            BarSpacing = 8,
            RightOffset = 5
        };
        vs.PriceScale.MinPrice = 1.09;
        vs.PriceScale.MaxPrice = 1.12;
        return (vs, new CoordinateConverter(vs, 50));
    }

    [Fact]
    public void MarketLine_DrawsDashedLineAndMarker()
    {
        var (vs, cc) = Setup();
        var renderer = new CurrentPriceRenderer(new ThemeService());
        var ctx = new RecordingRenderContext();

        renderer.Render(ctx, vs, cc, price: 1.1050, isBull: true, countdownText: "0:45", digits: 5, dpiScale: 1);

        Assert.Contains(ctx.Operations, o => o.StartsWith("DrawLine:"));
        Assert.Contains(ctx.Operations, o => o == "FillRect");
        Assert.Contains(ctx.Operations, o => o.StartsWith("DrawText:"));
    }

    [Fact]
    public void BearDirection_StillDraws()
    {
        var (vs, cc) = Setup();
        var renderer = new CurrentPriceRenderer(new ThemeService());
        var ctx = new RecordingRenderContext();
        renderer.Render(ctx, vs, cc, price: 1.1000, isBull: false, countdownText: null, digits: 5, dpiScale: 1);
        Assert.True(ctx.Operations.Count >= 2);
    }

    [Fact]
    public void AnalysisMarker_DrawnSeparately()
    {
        var (vs, cc) = Setup();
        var renderer = new CurrentPriceRenderer(new ThemeService());
        var ctx = new RecordingRenderContext();
        double y = cc.Y(1.1080);
        renderer.RenderAnalysisMarker(ctx, vs, y, price: 1.1080, digits: 5, dpiScale: 1);
        Assert.Contains(ctx.Operations, o => o == "FillRect");
        Assert.Contains(ctx.Operations, o => o.StartsWith("DrawText:"));
    }

    [Fact]
    public void DualMarkers_MarketThenAnalysis()
    {
        var (vs, cc) = Setup();
        var renderer = new CurrentPriceRenderer(new ThemeService());
        var ctx = new RecordingRenderContext();
        renderer.Render(ctx, vs, cc, 1.1050, isBull: true, "1:00", 5, 1);
        renderer.RenderAnalysisMarker(ctx, vs, cc.Y(1.1070), 1.1070, 5, 1);
        Assert.True(ctx.Operations.Count(o => o == "FillRect") >= 2);
        Assert.True(ctx.Operations.Count(o => o.StartsWith("DrawText:")) >= 2);
    }
}
