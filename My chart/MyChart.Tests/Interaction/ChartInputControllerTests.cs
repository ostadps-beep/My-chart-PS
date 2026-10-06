using MyChart.Core.Models.Viewport;
using MyChart.Core.Scale;
using MyChart.Interaction.Input;
using Xunit;

namespace MyChart.Tests.Interaction;

public class ChartInputControllerTests
{
    private static ViewState Vs()
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
        return vs;
    }

    [Fact]
    public void HitTest_Regions()
    {
        var vs = Vs();
        Assert.Equal(HitRegion.Plot, InputHitTest.RegionAt(vs, 100, 100));
        Assert.Equal(HitRegion.PriceAxis, InputHitTest.RegionAt(vs, 850, 100));
        Assert.Equal(HitRegion.TimeAxis, InputHitTest.RegionAt(vs, 100, 490));
    }

    [Fact]
    public void WheelOnPlot_Zooms()
    {
        var vs = Vs();
        double before = vs.BarSpacing;
        var c = new ChartInputController { BarCount = 100 };
        var action = c.OnPointer(vs, new PointerInput(
            PointerPhase.Wheel, PointerButton.None, 200, 100, WheelDelta: 120, Ctrl: false, Shift: false, Space: false));
        Assert.Equal(ChartInputAction.Zoom, action);
        Assert.True(vs.BarSpacing > before); // zoom in increases spacing
    }

    [Fact]
    public void CtrlWheel_PrecisionZoom()
    {
        var vs = Vs();
        var c = new ChartInputController { BarCount = 100 };
        var action = c.OnPointer(vs, new PointerInput(
            PointerPhase.Wheel, PointerButton.None, 200, 100, 120, Ctrl: true, Shift: false, Space: false));
        Assert.Equal(ChartInputAction.PrecisionZoom, action);
    }

    [Fact]
    public void MiddleDrag_Pans()
    {
        var vs = Vs();
        double offsetBefore = vs.RightOffset;
        var c = new ChartInputController { BarCount = 100 };
        c.OnPointer(vs, new PointerInput(PointerPhase.Down, PointerButton.Middle, 200, 100, 0, false, false, false));
        c.OnPointer(vs, new PointerInput(PointerPhase.Move, PointerButton.Middle, 220, 100, 0, false, false, false));
        Assert.Equal(ChartInputAction.Pan, c.LastAction);
        Assert.NotEqual(offsetBefore, vs.RightOffset);
    }

    [Fact]
    public void DoubleClickPriceAxis_FitAuto()
    {
        var vs = Vs();
        vs.PriceScale.Fit = ScaleFit.Manual;
        var c = new ChartInputController { BarCount = 100 };
        var action = c.OnPointer(vs, new PointerInput(
            PointerPhase.DoubleClick, PointerButton.Left, 850, 100, 0, false, false, false));
        Assert.Equal(ChartInputAction.FitAuto, action);
        Assert.Equal(ScaleFit.Auto, vs.PriceScale.Fit);
    }

    [Fact]
    public void Keys_EscHome()
    {
        var vs = Vs();
        var c = new ChartInputController { BarCount = 100 };
        Assert.Equal(ChartInputAction.CancelTool, c.OnKey(vs, new KeyInput("Escape", false, false, false)));
        Assert.Equal(ChartInputAction.GoToLatest, c.OnKey(vs, new KeyInput("Home", false, false, false)));
        Assert.Equal(ChartInputAction.LatestMarketPosition, c.OnKey(vs, new KeyInput("Home", Ctrl: true, false, false)));
        Assert.Equal(ChartInputAction.NavigationDialog, c.OnKey(vs, new KeyInput("G", Ctrl: true, false, false)));
    }

    [Fact]
    public void RightClick_ContextMenu()
    {
        var vs = Vs();
        var c = new ChartInputController { BarCount = 100 };
        var action = c.OnPointer(vs, new PointerInput(
            PointerPhase.Down, PointerButton.Right, 100, 100, 0, false, false, false));
        Assert.Equal(ChartInputAction.ContextMenu, action);
    }

    [Fact]
    public void ShiftClick_MultiSelect()
    {
        var vs = Vs();
        var c = new ChartInputController { BarCount = 100 };
        var action = c.OnPointer(vs, new PointerInput(
            PointerPhase.Down, PointerButton.Left, 100, 100, 0, false, Shift: true, false));
        Assert.Equal(ChartInputAction.MultiSelect, action);
    }
}
