using MyChart.Core.Analysis;
using MyChart.Core.Models.Market;
using MyChart.Core.Models.Viewport;
using MyChart.Core.Scale;
using MyChart.Core.UI.Navigator;
using Xunit;

namespace MyChart.Tests.UI;

/// <summary>T6.07 NavigatorPanel — visibility, overview, GoTo routing.</summary>
public class NavigatorPanelTests
{
    private static readonly DateTimeOffset T0 = new(2024, 6, 1, 12, 0, 0, TimeSpan.Zero);

    private static ViewState Vs() => new()
    {
        Width = 864,
        Height = 500,
        PriceAxisWidth = 64,
        TimeAxisHeight = 24,
        BarSpacing = 8,
        RightOffset = 5
    };

    [Fact]
    public void Default_Hidden()
    {
        var model = new NavigatorPanelModel();
        Assert.False(model.IsVisible(T0));
    }

    [Fact]
    public void Show_VisibleFor5Seconds()
    {
        var model = new NavigatorPanelModel();
        model.Show(T0);
        Assert.True(model.IsVisible(T0.AddSeconds(4.9)));
        Assert.False(model.IsVisible(T0.AddSeconds(5.0)));
    }

    [Fact]
    public void Interact_RestartsTimer()
    {
        var model = new NavigatorPanelModel();
        model.Show(T0);
        model.Interact(T0.AddSeconds(4));
        Assert.True(model.IsVisible(T0.AddSeconds(8)));
        Assert.False(model.IsVisible(T0.AddSeconds(9.1)));
    }

    [Fact]
    public void Toggle_OpensAndCloses()
    {
        var ctl = new NavigatorPanelController(new NavigatorPanelModel());
        ctl.Toggle(T0);
        Assert.True(ctl.Model.IsVisible(T0));
        ctl.Toggle(T0.AddSeconds(1));
        Assert.False(ctl.Model.IsVisible(T0.AddSeconds(1)));
    }

    [Fact]
    public void Overview_MatchesColumnCount()
    {
        var bars = new List<Candle>();
        for (int i = 0; i < 50; i++)
            bars.Add(new Candle(T0.AddMinutes(i), 1.1, 1.11, 1.09, 1.1 + i * 0.0001, 1));

        var model = new NavigatorPanelModel { OverviewColumns = 10 };
        model.ComputeOverview(bars);
        Assert.Equal(10, model.Overview.Length);
    }

    [Fact]
    public void GoToCandle_CentersAndKeepsVisible()
    {
        var vs = Vs();
        var ctl = new NavigatorPanelController(new NavigatorPanelModel());
        ctl.Open(T0);
        var result = ctl.GoToCandle(vs, n: 100, barsBack: 10, T0.AddSeconds(1));
        Assert.Equal(NavigateOutcome.Centered, result.Outcome);
        Assert.True(ctl.Model.IsVisible(T0.AddSeconds(5.5))); // timer restarted
    }

    [Fact]
    public void Hide_Immediate()
    {
        var ctl = new NavigatorPanelController(new NavigatorPanelModel());
        ctl.Open(T0);
        ctl.Close();
        Assert.False(ctl.Model.IsVisible(T0));
    }
}
