using MyChart.Core.Models.Viewport;
using MyChart.Core.Scale;
using Xunit;

namespace MyChart.Tests.Scale;

/// <summary>T2.06 VERIFY — GoToLatest centres last bar; lock keeps RightOffset.</summary>
public class LatestViewControllerTests
{
    private static ViewState CreateVs(double plotWidth = 800, double barSpacing = 8)
        => new()
        {
            Width = plotWidth + 64,
            Height = 400,
            PriceAxisWidth = 64,
            PriceAxisSide = PriceAxisSide.Right,
            BarSpacing = barSpacing,
            RightOffset = 5
        };

    [Fact]
    public void GoToLatest_CentresLastBar()
    {
        var vs = CreateVs();
        int n = 100;
        LatestViewController.GoToLatest(vs);

        var cc = new CoordinateConverter(vs, n);
        double xLast = cc.X(n - 1);
        double centre = vs.PlotLeft + vs.PlotWidth / 2.0;
        Assert.Equal(centre, xLast, 9);
    }

    [Fact]
    public void Lock_KeepsRightOffset_AfterEvents()
    {
        var vs = CreateVs();
        vs.RightOffset = 5;
        var ctrl = new LatestViewController();
        ctrl.EnableLock(vs);

        vs.RightOffset = 20; // would-be pan
        ctrl.OnCandleEvent(vs);
        Assert.Equal(5, vs.RightOffset, 9);

        // simulate many appends
        for (int i = 0; i < 100; i++)
        {
            vs.RightOffset = i;
            ctrl.OnCandleEvent(vs);
        }
        Assert.Equal(5, vs.RightOffset, 9);
        Assert.False(ctrl.AllowHorizontalPan);
    }

    [Fact]
    public void InfiniteScroll_Trigger()
    {
        var ctrl = new LatestViewController();
        Assert.True(ctrl.ShouldRequestOlderHistory(renderFrom: 100));
        Assert.False(ctrl.ShouldRequestOlderHistory(renderFrom: 250));

        ctrl.HistoryRequestInFlight = true;
        Assert.False(ctrl.ShouldRequestOlderHistory(100));

        ctrl.HistoryRequestInFlight = false;
        ctrl.ReachedStart = true;
        Assert.False(ctrl.ShouldRequestOlderHistory(100));
    }
}
