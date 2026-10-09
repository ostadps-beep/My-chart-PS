using MyChart.Core.Models.Settings;
using Xunit;

namespace MyChart.Tests.Settings;

/// <summary>T6.08 VERIFY — key routing, NOT_IN_V1, seed, one invalidation per OnApplied.</summary>
public class SettingsBridgeTests
{
    [Theory]
    [InlineData("chart.offline", true)]
    [InlineData("chart.shift", false)]
    [InlineData("chart.autoscroll", false)]
    [InlineData("grid.show", false)]
    [InlineData("performance.anti.aliasing", false)]
    [InlineData("drawing.tools.show.labels", false)]
    public void BoolKeys_Route(string key, bool value)
    {
        var b = new SettingsBridge();
        var seen = new List<string>();
        b.Changed += keys => seen.AddRange(keys);
        b.OnUpdate(key, value);
        Assert.Contains(key, seen);
        Assert.True(SettingsKeyRouter.TryApply(b.Values, key, value) == false); // already set
    }

    [Theory]
    [InlineData("chart.zoom.speed", 80)]
    [InlineData("chart.visible.candles", 200)]
    [InlineData("hud.overlay.hud.transparency", 40)]
    [InlineData("performance.fps.limit", 30)]
    public void IntKeys_Route(string key, int value)
    {
        var b = new SettingsBridge();
        b.OnUpdate(key, value);
        object actual = key switch
        {
            "chart.zoom.speed" => b.Values.ZoomSpeed,
            "chart.visible.candles" => b.Values.VisibleCandles,
            "hud.overlay.hud.transparency" => b.Values.HudTransparency,
            "performance.fps.limit" => b.Values.FpsLimit,
            _ => -1
        };
        Assert.Equal(value, actual);
    }

    [Theory]
    [InlineData("chart.zoom.behavior", "Price")]
    [InlineData("chart.display.mode", "OHLC")]
    [InlineData("axes.price.position", "Left")]
    [InlineData("hud.overlay.hud.position", "TopRight")]
    public void StringKeys_Route(string key, string value)
    {
        var b = new SettingsBridge();
        b.OnUpdate(key, value);
        string actual = key switch
        {
            "chart.zoom.behavior" => b.Values.ZoomBehavior,
            "chart.display.mode" => b.Values.DisplayMode,
            "axes.price.position" => b.Values.PriceAxisPosition,
            "hud.overlay.hud.position" => b.Values.HudPosition,
            _ => ""
        };
        Assert.Equal(value, actual);
    }

    [Fact]
    public void UnsupportedOption_Ignored()
    {
        var b = new SettingsBridge();
        var before = b.Values.DisplayMode;
        b.OnUpdate("chart.display.mode", "Line"); // not in allowed set
        Assert.Equal(before, b.Values.DisplayMode);
    }

    [Fact]
    public void NotInV1_NoError_NoChange()
    {
        var b = new SettingsBridge();
        var seen = new List<string>();
        b.Changed += k => seen.AddRange(k);
        // workspace.* commands are NOT_IN_V1 for chart consumers (R7 / R10)
        b.OnUpdate("workspace.reset", true);
        b.OnUpdate("workspace.import", true);
        b.OnUpdate("workspace.export", true);
        Assert.Empty(seen);
    }

    [Fact]
    public void ThemeProfile_Routes_After_T702()
    {
        var b = new SettingsBridge();
        b.OnUpdate("theme.profile", "Light");
        Assert.Equal("Light", b.Values.ThemeProfileName);
    }

    [Fact]
    public void UnknownKey_Ignored()
    {
        var b = new SettingsBridge();
        b.OnUpdate("totally.unknown.key", 1);
        Assert.Equal(0, b.InvalidationCount);
    }

    [Fact]
    public void OnApplied_OneInvalidation()
    {
        var b = new SettingsBridge();
        b.OnUpdate("chart.offline", true);
        b.OnUpdate("grid.show", false);
        Assert.Equal(0, b.InvalidationCount);
        b.OnApplied();
        Assert.Equal(1, b.InvalidationCount);
        b.OnApplied();
        Assert.Equal(2, b.InvalidationCount);
    }

    [Fact]
    public void Seed_RaisesAllKnownKeys()
    {
        var b = new SettingsBridge();
        var seen = new HashSet<string>();
        b.Changed += keys => { foreach (var k in keys) seen.Add(k); };
        b.Seed();
        Assert.Equal(SettingsKeyRouter.KnownKeys.Count, seen.Count);
        Assert.True(SettingsKeyRouter.KnownKeys.SetEquals(seen));
    }

    [Fact]
    public void EveryKnownKey_HasRoutingPath()
    {
        var v = new ChartSettingValues();
        foreach (var key in SettingsKeyRouter.KnownKeys)
        {
            // Applying current-compatible value should not throw
            object sample = key switch
            {
                _ when key.Contains("speed") || key.Contains("size") || key.Contains("limit")
                    || key.Contains("transparency") || key.Contains("candles") && key.Contains("visible")
                    => 42,
                _ when key.Contains("spacing") || key.Contains("minimum") || key.Contains("maximum")
                    => 1.5,
                _ when key.Contains("color") || key.Contains("items") || key.Contains("type")
                    || key.Contains("level") || key.Contains("behavior") || key.Contains("mode")
                    || key.Contains("position") || key.Contains("style") || key.Contains("wheel")
                    => key switch
                    {
                        "chart.zoom.behavior" => "Both",
                        "chart.mouse.wheel" => "Zoom",
                        "chart.display.mode" => "Candlesticks",
                        "grid.style" => "Solid",
                        "axes.price.position" => "Right",
                        "hud.overlay.hud.position" => "TopLeft",
                        "performance.log.level" => "Info",
                        "performance.error.behavior" => "Continue",
                        "grid.horizontal.color" => "#2A2E39",
                        "hud.overlay.hud.text.color" => "#FFFFFF",
                        "hud.overlay.hud.font.type" => "Segoe UI",
                        "hud.overlay.hud.items" => "OHLC",
                        _ => "x"
                    },
                _ => true
            };
            // Must not throw
            SettingsKeyRouter.TryApply(v, key, sample);
        }
    }
}
