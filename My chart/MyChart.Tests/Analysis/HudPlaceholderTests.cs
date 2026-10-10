using MyChart.Core.Analysis;
using MyChart.Core.Models.Chart;
using MyChart.Core.Models.Market;
using MyChart.Core.Models.Viewport;
using Xunit;

namespace MyChart.Tests.Analysis;

/// <summary>C3 — FPS and session fields must not rely on a hard-coded zero FPS in Compute.</summary>
public class HudPlaceholderTests
{
    private static HudInput Base(
        double fps,
        bool connected = true,
        string provider = "csv")
    {
        var symbol = new SymbolInfo("EURUSD", "csv", SymbolGroup.Forex, 5);
        var bars = new List<Candle>
        {
            new(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero), 1.1, 1.11, 1.09, 1.105, 1)
        };
        var range = new RenderWindowRange(0, 0, 0, 0, 1);
        return new HudInput(
            Symbol: symbol,
            Timeframe: Timeframe.M1,
            Bars: bars,
            Range: range,
            BarSpacing: 8,
            PlotWidth: 1000,
            Crosshair: null,
            LastTick: null,
            ServerNow: DateTimeOffset.UtcNow,
            ReplayActive: false,
            ProviderConnected: connected,
            ProviderName: provider,
            CacheStatus: HudCacheStatus.Memory,
            Fps: fps,
            EnabledAdvanced: HudField.Fps);
    }

    [Fact]
    public void Compute_PassesThroughMeasuredFps()
    {
        var state = HudDataProvider.Compute(Base(fps: 58.3));
        Assert.Equal(58.3, state.Fps);
    }

    [Fact]
    public void Compute_ProviderName_IsPassThrough()
    {
        var state = HudDataProvider.Compute(Base(fps: 30, provider: "csv"));
        Assert.Equal("csv", state.ProviderName);
    }

    [Fact]
    public void Fps_IsAdvancedField_NotAlwaysVisible()
    {
        var without = HudDataProvider.VisibleFields(HudPresentation.Full, HudField.None);
        Assert.False(without.HasFlag(HudField.Fps));

        var with = HudDataProvider.VisibleFields(HudPresentation.Full, HudField.Fps);
        Assert.True(with.HasFlag(HudField.Fps));
    }
}
