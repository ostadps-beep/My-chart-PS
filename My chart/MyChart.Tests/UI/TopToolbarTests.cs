using MyChart.Core.Models.Market;
using MyChart.Core.UI.Toolbar;
using Xunit;

namespace MyChart.Tests.UI;

/// <summary>T6.03 TopToolbar — inventory, height, 2-click rule (GATE=VISUAL pending).</summary>
public class TopToolbarTests
{
    [Fact]
    public void Height_Is_32_Dip()
    {
        Assert.Equal(32, TopToolbarModel.HeightDip);
    }

    [Fact]
    public void Item_Order_Is_Exact()
    {
        var tb = TopToolbarModel.CreateDefault();
        Assert.Equal(
            new[]
            {
                TopToolbarItemKind.Symbol,
                TopToolbarItemKind.Timeframe,
                TopToolbarItemKind.ChartType,
                TopToolbarItemKind.Indicators,
                TopToolbarItemKind.Templates,
                TopToolbarItemKind.Layout,
                TopToolbarItemKind.Settings
            },
            tb.Items.Select(i => i.Kind).ToArray());
    }

    [Fact]
    public void Timeframe_Quick_And_Dropdown()
    {
        var tf = TopToolbarModel.CreateDefault().Items.Single(i => i.Kind == TopToolbarItemKind.Timeframe);
        Assert.Equal(ToolbarPopupKind.QuickButtonsWithDropdown, tf.Popup);

        var quick = tf.Choices.Where(c => c.IsQuick).Select(c => c.Value).ToArray();
        Assert.Equal(new[] { "M1", "M5", "M15", "H1", "H4", "D1" }, quick);

        var drop = tf.Choices.Where(c => !c.IsQuick).Select(c => c.Value).ToArray();
        Assert.Equal(new[] { "M30", "W1", "MN1" }, drop);
    }

    [Fact]
    public void ChartType_Three_Items()
    {
        var ct = TopToolbarModel.CreateDefault().Items.Single(i => i.Kind == TopToolbarItemKind.ChartType);
        Assert.Equal(3, ct.Choices.Count);
        Assert.Equal(new[] { "Candles", "HollowCandles", "OHLC" }, ct.Choices.Select(c => c.Value).ToArray());
    }

    [Fact]
    public void Layout_Values_1_2_4_6_8()
    {
        var ly = TopToolbarModel.CreateDefault().Items.Single(i => i.Kind == TopToolbarItemKind.Layout);
        Assert.Equal(new[] { "1", "2", "4", "6", "8" }, ly.Choices.Select(c => c.Value).ToArray());
    }

    [Fact]
    public void Settings_Disabled_Until_T608()
    {
        var off = TopToolbarModel.CreateDefault(settingsAvailable: false)
            .Items.Single(i => i.Kind == TopToolbarItemKind.Settings);
        Assert.False(off.IsEnabled);

        var on = TopToolbarModel.CreateDefault(settingsAvailable: true)
            .Items.Single(i => i.Kind == TopToolbarItemKind.Settings);
        Assert.True(on.IsEnabled);
    }

    [Fact]
    public void Symbol_Is_SearchList_Indicators_Is_Dialog()
    {
        var tb = TopToolbarModel.CreateDefault();
        Assert.Equal(ToolbarPopupKind.SearchList, tb.Items.Single(i => i.Kind == TopToolbarItemKind.Symbol).Popup);
        Assert.Equal(ToolbarPopupKind.Dialog, tb.Items.Single(i => i.Kind == TopToolbarItemKind.Indicators).Popup);
        Assert.Equal(ToolbarPopupKind.Window, tb.Items.Single(i => i.Kind == TopToolbarItemKind.Settings).Popup);
    }

    [Fact]
    public void TwoClick_Rule_Open_Then_Apply()
    {
        var tb = TopToolbarModel.CreateDefault(selectedTf: Timeframe.M1);
        var tf = tb.Items.Single(i => i.Kind == TopToolbarItemKind.Timeframe);
        var ctrl = new TopToolbarController();

        Assert.False(ctrl.Apply(tf, "H1"));
        Assert.True(ctrl.Open(tf));
        Assert.Equal(TopToolbarItemKind.Timeframe, ctrl.OpenPopup);
        Assert.True(ctrl.Apply(tf, "H1"));
        Assert.Null(ctrl.OpenPopup);
        Assert.Equal(new[] { "Timeframe:H1" }, ctrl.AppliedActions.ToArray());
    }

    [Fact]
    public void Disabled_Settings_Cannot_Open()
    {
        var settings = TopToolbarModel.CreateDefault(settingsAvailable: false)
            .Items.Single(i => i.Kind == TopToolbarItemKind.Settings);
        var ctrl = new TopToolbarController();
        Assert.False(ctrl.Open(settings));
        Assert.Null(ctrl.OpenPopup);
    }

    [Fact]
    public void Templates_Save_And_Load()
    {
        var tpl = TopToolbarModel.CreateDefault().Items.Single(i => i.Kind == TopToolbarItemKind.Templates);
        Assert.Equal(ToolbarPopupKind.ActionList, tpl.Popup);
        Assert.Equal(new[] { "Save", "Load" }, tpl.Choices.Select(c => c.Value).ToArray());
    }
}
