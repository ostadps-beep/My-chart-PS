using MyChart.Core.Contracts.Plugins;
using MyChart.Core.Models.Plugins;
using MyChart.Core.Plugins.Identity;
using MyChart.Core.Plugins.Manifest;
using MyChart.Core.Services;
using MyChart.PluginHost.Guard;
using MyChart.PluginHost.Loading;
using Xunit;

namespace MyChart.Tests.PluginHost;

public class PluginHostLoadTests
{
    private sealed class GoodPlugin : IChartPlugin
    {
        public string ComponentId => "GoodTool";
        public void Register(IPluginHost host)
        {
            host.Tools.Register(
                new ToolDescriptor("GoodTool", "GoodTool", "Good", "Draw", "Icon.Good", null, "drawing.good", 2, Array.Empty<ParameterDescriptor>()),
                _ => null!);
        }
    }

    private sealed class ThrowingPlugin : IChartPlugin
    {
        public string ComponentId => "BadTool";
        public void Register(IPluginHost host) => throw new InvalidOperationException("boom");
    }

    private sealed class DupPlugin : IChartPlugin
    {
        // ComponentId sorts after GoodTool so GoodTool registers first; this is the "second" duplicate ToolId.
        public string ComponentId => "ZDupTool";
        public void Register(IPluginHost host)
        {
            host.Tools.Register(
                new ToolDescriptor("ZDupTool", "GoodTool", "Dup", "Draw", "Icon.Good", null, "drawing.dup", 2, Array.Empty<ParameterDescriptor>()),
                _ => null!);
        }
    }

    [Fact]
    public void GoodPlugin_Loads()
    {
        var host = new PluginHostService();
        var source = new CatalogSource(() => new[]
        {
            new ComponentSet
            {
                ComponentId = "GoodTool",
                Plugin = new GoodPlugin(),
                Icons = new[] { new IconDescriptor("Icon.Good", "M0 0 L10 10") }
            }
        });
        var report = host.Load(new[] { source });
        Assert.Contains("GoodTool", report.Loaded);
        Assert.Empty(report.Failed);
        Assert.True(host.Tools.TryGet("GoodTool", out _, out _));
    }

    [Fact]
    public void ThrowingPlugin_Isolated_DoesNotStopOthers()
    {
        var host = new PluginHostService();
        var source = new CatalogSource(() => new[]
        {
            new ComponentSet { ComponentId = "BadTool", Plugin = new ThrowingPlugin() },
            new ComponentSet
            {
                ComponentId = "GoodTool",
                Plugin = new GoodPlugin(),
                Icons = new[] { new IconDescriptor("Icon.Good", "M0 0 L1 1") }
            }
        });
        var report = host.Load(new[] { source });
        Assert.Contains("GoodTool", report.Loaded);
        Assert.Contains(report.Failed, f => f.ComponentId == "BadTool");
    }

    [Fact]
    public void DuplicateToolId_DisablesSecond()
    {
        var host = new PluginHostService();
        var source = new CatalogSource(() => new[]
        {
            new ComponentSet
            {
                ComponentId = "GoodTool",
                Plugin = new GoodPlugin(),
                Icons = new[] { new IconDescriptor("Icon.Good", "M0 0 L1 1") }
            },
            new ComponentSet { ComponentId = "ZDupTool", Plugin = new DupPlugin() }
        });
        var report = host.Load(new[] { source });
        Assert.Contains("GoodTool", report.Loaded);
        Assert.Contains(report.Failed, f => f.ComponentId == "ZDupTool" && f.Code == ErrorCodes.DuplicateToolId);
    }

    [Fact]
    public void CircuitBreaker_ThreeFaultsIn60s_Opens()
    {
        var clock = new FixedClock(DateTimeOffset.Parse("2024-01-01T00:00:00Z"));
        var breaker = new CircuitBreaker(clock);
        breaker.RecordFault();
        clock.UtcNow = clock.UtcNow.AddSeconds(10);
        breaker.RecordFault();
        clock.UtcNow = clock.UtcNow.AddSeconds(10);
        breaker.RecordFault();
        Assert.True(breaker.IsOpen);
    }

    [Fact]
    public void CircuitBreaker_FiveSlowFrames_Opens()
    {
        var breaker = new CircuitBreaker(new FixedClock(DateTimeOffset.UtcNow));
        for (int i = 0; i < 4; i++)
            breaker.RecordFrame(9);
        Assert.False(breaker.IsOpen);
        breaker.RecordFrame(9);
        Assert.True(breaker.IsOpen);
    }

    [Fact]
    public void IdRules_AcceptanceVectors()
    {
        Assert.Equal(ErrorCodes.InvalidId, IdRules.Validate("trendline"));
        Assert.Equal(ErrorCodes.InvalidId, IdRules.Validate("Trend Line"));
        Assert.Equal(ErrorCodes.InvalidId, IdRules.Validate("TL"));
        Assert.Null(IdRules.Validate("TrendLine"));
    }

    [Fact]
    public void HotkeyRules_Reserved()
    {
        Assert.Equal(ErrorCodes.HotkeyReserved, HotkeyRules.Validate("Esc"));
        Assert.Equal(ErrorCodes.HotkeyReserved, HotkeyRules.Validate("Ctrl+Z"));
        Assert.Null(HotkeyRules.Validate("Ctrl+Q"));
        Assert.Null(HotkeyRules.Validate("T"));
    }

    [Fact]
    public void GeometryPath_Vectors()
    {
        Assert.Null(GeometryPathGrammar.Validate("M2 2 L14 14"));
        Assert.Equal(ErrorCodes.InvalidGeometry, GeometryPathGrammar.Validate("M2 2 X14 14"));
        Assert.Equal(ErrorCodes.InvalidGeometry, GeometryPathGrammar.Validate(""));
    }
}
