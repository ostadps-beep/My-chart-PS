using MyChart.Core.Models.Plugins;
using MyChart.Core.UI.Toolbar;
using Xunit;

namespace MyChart.Tests.UI;

/// <summary>T6.05 ContextToolbar — visibility, gap 8 DIP, items, placement, EditDrawing mapping (GATE=VISUAL pending).</summary>
public class ContextToolbarTests
{
    [Fact]
    public void Gap_Is_8_Dip_Button_32()
    {
        Assert.Equal(8, ContextToolbarModel.GapDip);
        Assert.Equal(32, ContextToolbarModel.ButtonSizeDip);
    }

    [Fact]
    public void Hidden_When_Selection_Empty()
    {
        var m = ContextToolbarModel.Create(
            selectionCount: 0,
            selectionLeftDip: 100, selectionTopDip: 200,
            selectionRightDip: 180, selectionBottomDip: 260,
            viewportTopDip: 0, viewportBottomDip: 500);
        Assert.False(m.IsVisible);
    }

    [Fact]
    public void Visible_When_Selection_Not_Empty()
    {
        var m = ContextToolbarModel.Create(
            selectionCount: 1,
            selectionLeftDip: 100, selectionTopDip: 200,
            selectionRightDip: 180, selectionBottomDip: 260,
            viewportTopDip: 0, viewportBottomDip: 500);
        Assert.True(m.IsVisible);
    }

    [Fact]
    public void Default_Items_Order_0_to_70()
    {
        var m = ContextToolbarModel.Create(1, 0, 100, 50, 150, 0, 400);
        Assert.Equal(8, m.Items.Count);
        Assert.Equal(
            new[]
            {
                ContextToolbarAction.Color,
                ContextToolbarAction.Width,
                ContextToolbarAction.Opacity,
                ContextToolbarAction.Style,
                ContextToolbarAction.Template,
                ContextToolbarAction.Lock,
                ContextToolbarAction.Clone,
                ContextToolbarAction.Delete
            },
            m.Items.Select(i => i.Action).ToArray());
        Assert.Equal(new[] { 0, 10, 20, 30, 40, 50, 60, 70 }, m.Items.Select(i => i.Order).ToArray());
    }

    [Fact]
    public void Prefers_Above_When_Room()
    {
        // selection top 200, bar needs 8+32=40 above -> 160 >= viewport 0
        var m = ContextToolbarModel.Create(1, 100, 200, 180, 260, 0, 500);
        Assert.Equal(ContextToolbarPlacement.Above, m.Placement);
        Assert.Equal(200 - 8 - 32, m.AnchorYDip);
        Assert.Equal(140, m.AnchorXDip); // center of 100..180
    }

    [Fact]
    public void Places_Below_When_No_Room_Above()
    {
        // selection top 20 -> above would be 20-8-32=-20 < viewport 0
        var m = ContextToolbarModel.Create(1, 100, 20, 180, 80, 0, 500);
        Assert.Equal(ContextToolbarPlacement.Below, m.Placement);
        Assert.Equal(80 + 8, m.AnchorYDip);
    }

    [Fact]
    public void Core_Contributions_Are_ContextToolbar_Slot()
    {
        Assert.All(ContextToolbarModel.CoreContributions, c =>
        {
            Assert.Equal(Slot.ContextToolbar, c.Slot);
            Assert.False(string.IsNullOrWhiteSpace(c.IconKey));
            Assert.False(string.IsNullOrWhiteSpace(c.CommandRef));
        });
    }

    [Fact]
    public void Controller_Issues_EditDrawing_For_Style_Actions()
    {
        var ctrl = new ContextToolbarController();
        var color = ContextToolbarModel.DefaultItems.Single(i => i.Action == ContextToolbarAction.Color);
        Assert.True(ctrl.TryIssue(color, selectionCount: 1));
        Assert.Equal("EditDrawing:Color", ctrl.IssuedCommands[0]);
    }

    [Fact]
    public void Controller_Issues_Clone_And_Delete()
    {
        var ctrl = new ContextToolbarController();
        var clone = ContextToolbarModel.DefaultItems.Single(i => i.Action == ContextToolbarAction.Clone);
        var del = ContextToolbarModel.DefaultItems.Single(i => i.Action == ContextToolbarAction.Delete);
        Assert.True(ctrl.TryIssue(clone, 1));
        Assert.True(ctrl.TryIssue(del, 1));
        Assert.Equal(new[] { "CloneDrawing", "RemoveDrawings" }, ctrl.IssuedCommands.ToArray());
    }

    [Fact]
    public void Controller_Ignores_When_No_Selection()
    {
        var ctrl = new ContextToolbarController();
        var lockItem = ContextToolbarModel.DefaultItems.Single(i => i.Action == ContextToolbarAction.Lock);
        Assert.False(ctrl.TryIssue(lockItem, selectionCount: 0));
        Assert.Empty(ctrl.IssuedCommands);
    }
}
