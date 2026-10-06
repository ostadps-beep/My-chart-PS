using MyChart.Core.Models.Viewport;
using MyChart.Core.Scale;
using MyChart.Core.Services;
using MyChart.Rendering.Layers;
using MyChart.Tests.Support;
using Xunit;

namespace MyChart.Tests.Rendering;

/// <summary>
/// T4.03 GATE=VISUAL — generates PNG under artifacts/visual/T4.03/ for owner approval.
/// </summary>
public class T403VisualProofTests
{
    private static ViewState SceneView()
    {
        var vs = new ViewState
        {
            Width = 800,
            Height = 500,
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
    public void T403_BackgroundGridAxes_Png_IsDeterministic_AndWritten()
    {
        var theme = new ThemeService();
        var vs = SceneView();
        var converter = new CoordinateConverter(vs, 100);
        var minT = vs.PriceScale.TransformPrice(vs.PriceScale.MinPrice);
        var maxT = vs.PriceScale.TransformPrice(vs.PriceScale.MaxPrice);
        var step = NiceTicks.ComputeStep(maxT - minT, vs.PlotHeight, 0.00001);
        var ticks = NiceTicks.PriceTicks(minT, maxT, step);
        var timeUs = new double[] { 5, 20, 40, 60, 80 };
        var timeLabels = new (double, string)[]
        {
            (5, "10:00"), (20, "13:45"), (40, "18:30"), (60, "23:15"), (80, "04:00")
        };

        void Paint(MyChart.Core.Contracts.Rendering.IRenderContext ctx)
        {
            new BackgroundRenderer(theme).Render(ctx, vs, dpiScale: 1);
            new GridRenderer(theme).Render(ctx, vs, converter, ticks, timeUs, dpiScale: 1);
            new PriceAxisRenderer(theme).Render(ctx, vs, converter, ticks, step, digits: 5, dpiScale: 1);
            new TimeAxisRenderer(theme).Render(ctx, vs, converter, timeLabels, dpiScale: 1);
        }

        var a = VisualProof.Render("T4.03", "background-grid-axes", 800, 500, Paint, writeToDisk: true);
        var b = VisualProof.Render("T4.03", "background-grid-axes-b", 800, 500, Paint, writeToDisk: false);

        Assert.NotEmpty(a);
        Assert.True(a.AsSpan().SequenceEqual(b), "T4.03 scene must be byte-identical on rerun.");

        var path = Path.Combine(VisualProof.ArtifactsRoot, "T4.03", "background-grid-axes.png");
        Assert.True(File.Exists(path), $"Open this PNG for visual approval: {path}");
    }
}
