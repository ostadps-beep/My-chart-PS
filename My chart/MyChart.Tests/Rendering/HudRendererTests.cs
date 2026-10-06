using MyChart.Core.Contracts.Rendering;
using MyChart.Core.Models.Chart;
using MyChart.Core.Models.Market;
using MyChart.Core.Models.Rendering;
using MyChart.Core.Rendering;
using MyChart.Tests.Support;
using Xunit;

namespace MyChart.Tests.Rendering;

/// <summary>
/// T6.01 HudRenderer — automated checks + VisualProof PNG (GATE=VISUAL still needs owner approval).
/// </summary>
public class HudRendererTests
{
    private static HudState MinimalState() => new()
    {
        Symbol = "EURUSD",
        Timeframe = Timeframe.M15,
        Presentation = HudPresentation.Minimal,
        Visible = HudField.Symbol | HudField.Timeframe | HudField.Spread,
        SpreadPoints = 12,
        MarketStatus = HudMarketStatus.Live,
    };

    [Fact]
    public void Layer_Is_Hud()
    {
        Assert.Equal(RenderLayer.Hud, new HudRenderer().Layer);
    }

    [Fact]
    public void Opacity_Full_And_Idle()
    {
        Assert.Equal(1.0, HudRenderer.ComputeOpacity(0, idle: false), 5);
        Assert.Equal(0.40, HudRenderer.ComputeOpacity(0, idle: true), 5);
        Assert.Equal(0.5, HudRenderer.ComputeOpacity(50, idle: false), 5);
        Assert.Equal(0.2, HudRenderer.ComputeOpacity(50, idle: true), 5);
    }

    [Fact]
    public void ShouldIdle_After_5_Seconds()
    {
        Assert.False(HudRenderer.ShouldIdle(TimeSpan.FromSeconds(4.9)));
        Assert.True(HudRenderer.ShouldIdle(TimeSpan.FromSeconds(5)));
        Assert.True(HudRenderer.ShouldIdle(TimeSpan.FromSeconds(30)));
    }

    [Fact]
    public void BuildLines_Minimal_Includes_Symbol_Tf_Spread()
    {
        var lines = HudRenderer.BuildLines(MinimalState());
        Assert.Contains(lines, l => l.Contains("EURUSD"));
        Assert.Contains(lines, l => l.Contains("M15"));
        Assert.Contains(lines, l => l.Contains("Spread 12"));
    }

    [Fact]
    public void Startup_Minimal_Is_Visible()
    {
        var lines = HudRenderer.BuildLines(MinimalState());
        Assert.NotEmpty(lines);
    }

    [Fact]
    public void VisualProof_ByteIdentical_Twice()
    {
        var renderer = new HudRenderer
        {
            State = MinimalState(),
            Theme = ThemeTokens.Dark,
            Transparency = 0,
            Position = "TopLeft",
            IsIdle = false,
        };

        var a = VisualProof.Render("T6.01", "hud-minimal", 400, 200, ctx =>
        {
            ctx.Clear(ThemeTokens.Dark.BackgroundColor);
            renderer.Render(ctx);
        }, writeToDisk: true);

        var b = VisualProof.Render("T6.01", "hud-minimal-b", 400, 200, ctx =>
        {
            ctx.Clear(ThemeTokens.Dark.BackgroundColor);
            renderer.Render(ctx);
        }, writeToDisk: false);

        Assert.Equal(a, b);
        Assert.True(a.Length > 100);
    }

    [Fact]
    public void VisualProof_Idle_Differs_From_Active()
    {
        var state = MinimalState();
        byte[] Active()
        {
            var r = new HudRenderer { State = state, Theme = ThemeTokens.Dark, IsIdle = false };
            return VisualProof.Render("T6.01", "hud-active", 400, 200, ctx =>
            {
                ctx.Clear(ThemeTokens.Dark.BackgroundColor);
                r.Render(ctx);
            }, writeToDisk: true);
        }
        byte[] Idle()
        {
            var r = new HudRenderer { State = state, Theme = ThemeTokens.Dark, IsIdle = true };
            return VisualProof.Render("T6.01", "hud-idle", 400, 200, ctx =>
            {
                ctx.Clear(ThemeTokens.Dark.BackgroundColor);
                r.Render(ctx);
            }, writeToDisk: true);
        }

        Assert.NotEqual(Active(), Idle());
    }
}
