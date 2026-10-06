using MyChart.Core.Models.Chart;
using MyChart.Core.Models.Viewport;
using MyChart.Core.Services;
using MyChart.Rendering.Layers;
using MyChart.Tests.Plugins;
using Xunit;

namespace MyChart.Tests.Rendering;

public class CrosshairRendererTests
{
    private static ViewState Vs() => new()
    {
        Width = 864,
        Height = 500,
        PriceAxisWidth = 64,
        TimeAxisHeight = 24,
        BarSpacing = 8,
        RightOffset = 5
    };

    private static CrosshairState InsideState() => new()
    {
        IsInsidePlot = true,
        SnapIndex = 10,
        X = 200,
        Y = 150,
        Price = 1.10500,
        PriceLabel = "1.10500",
        TimeLabel = "12:30",
        TimeUtc = DateTimeOffset.UtcNow
    };

    [Fact]
    public void OutsidePlot_DrawsNothing()
    {
        var renderer = new CrosshairRenderer(new ThemeService());
        var ctx = new RecordingRenderContext();
        renderer.Render(ctx, Vs(), new CrosshairState { IsInsidePlot = false }, dpiScale: 1);
        Assert.Empty(ctx.Operations);
    }

    [Fact]
    public void Inside_DrawsCrossLinesAndLabels()
    {
        var renderer = new CrosshairRenderer(new ThemeService());
        var ctx = new RecordingRenderContext();
        renderer.Render(ctx, Vs(), InsideState(), dpiScale: 1);

        Assert.True(ctx.Operations.Count(o => o.StartsWith("DrawLine:")) >= 2);
        Assert.Contains(ctx.Operations, o => o.StartsWith("DrawText:"));
        Assert.Contains(ctx.Operations, o => o == "FillRect");
    }

    [Fact]
    public void WithAnalysis_DrawsExtraBox()
    {
        var state = InsideState() with
        {
            Analysis = new AnalysisValues(
                PriceDifference: 0.0025,
                PointDifference: 250,
                PipDifference: 25.0,
                TimeDifference: TimeSpan.FromMinutes(45),
                CandleCount: 3,
                PercentageChange: 0.23)
        };
        var renderer = new CrosshairRenderer(new ThemeService());
        var ctx = new RecordingRenderContext();
        renderer.Render(ctx, Vs(), state, dpiScale: 1);

        Assert.True(ctx.Operations.Count(o => o.StartsWith("DrawText:")) >= 4);
    }
}
