using MyChart.Core.Models.Settings;
using MyChart.Core.UI.Toolbar;
using Xunit;

namespace MyChart.Tests.UI;

/// <summary>T6.08 — Settings available after panel is linked; bridge seed/routing still pure Core.</summary>
public class SettingsIntegrationTests
{
    [Fact]
    public void TopToolbar_Settings_Enabled_When_Available()
    {
        var tb = TopToolbarModel.CreateDefault(settingsAvailable: true);
        var settings = tb.Items.Single(i => i.Kind == TopToolbarItemKind.Settings);
        Assert.True(settings.IsEnabled);
        Assert.Equal(ToolbarPopupKind.Window, settings.Popup);
    }

    [Fact]
    public void SettingsBridge_Seed_Raises_Changed()
    {
        var bridge = new SettingsBridge();
        IReadOnlyList<string>? keys = null;
        bridge.Changed += k => keys = k;
        bridge.Seed();
        Assert.NotNull(keys);
        Assert.NotEmpty(keys!);
    }

    [Fact]
    public void SettingsBridge_OnUpdate_Known_Key()
    {
        var bridge = new SettingsBridge();
        bridge.OnUpdate("chart.offline", true);
        Assert.True(bridge.Values.Offline);
    }
}
