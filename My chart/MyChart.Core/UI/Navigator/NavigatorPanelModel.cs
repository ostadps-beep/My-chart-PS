using MyChart.Core.Analysis;
using MyChart.Core.Models.Market;
using MyChart.Core.Models.Viewport;
using MyChart.Core.Scale;

namespace MyChart.Core.UI.Navigator;

/// <summary>
/// T6.07 NavigatorPanel — panel of T3.03.
/// Default hidden; open from context menu or Ctrl+G; auto-hide 5 s after last interaction.
/// </summary>
public sealed class NavigatorPanelModel
{
    public const double DefaultHeightDip = 64;
    public const int DefaultOverviewColumns = 120;

    private readonly NavigatorVisibility _visibility = new();

    public double HeightDip { get; set; } = DefaultHeightDip;
    public int OverviewColumns { get; set; } = DefaultOverviewColumns;

    /// <summary>Cached overview columns for the last ComputeOverview call.</summary>
    public OverviewColumn[] Overview { get; private set; } = Array.Empty<OverviewColumn>();

    /// <summary>Last visible-range box in panel DIP coordinates.</summary>
    public VisibleRangeBox VisibleBox { get; private set; }

    public bool IsVisible(DateTimeOffset now) => _visibility.IsVisible(now);

    public void Show(DateTimeOffset now) => _visibility.Show(now);

    public void Hide() => _visibility.Hide();

    /// <summary>Any interaction while visible restarts the 5 s timer.</summary>
    public void Interact(DateTimeOffset now) => _visibility.Interact(now);

    public void ComputeOverview(IReadOnlyList<Candle> bars)
        => Overview = NavigatorOverview.Compute(bars, OverviewColumns);

    public void UpdateVisibleBox(RenderWindowRange range, int barCount, double panelWidth)
        => VisibleBox = NavigatorCalculator.VisibleBox(range, barCount, panelWidth);
}

/// <summary>T6.07 controller — opens/closes panel and routes GoTo* + box drag.</summary>
public sealed class NavigatorPanelController
{
    private readonly NavigatorPanelModel _model;

    public NavigatorPanelController(NavigatorPanelModel model)
        => _model = model;

    public NavigatorPanelModel Model => _model;

    /// <summary>Ctrl+G or context-menu "Navigator".</summary>
    public void Toggle(DateTimeOffset now)
    {
        if (_model.IsVisible(now))
            _model.Hide();
        else
            _model.Show(now);
    }

    public void Open(DateTimeOffset now) => _model.Show(now);

    public void Close() => _model.Hide();

    public void NoteInteraction(DateTimeOffset now) => _model.Interact(now);

    public NavigateResult GoToCandle(ViewState vs, int n, int barsBack, DateTimeOffset now)
    {
        NoteInteraction(now);
        return NavigatorCalculator.GoToCandle(vs, n, barsBack);
    }

    public NavigateResult GoToTimeUtc(
        ViewState vs,
        TimeIndexMapper mapper,
        DateTimeOffset timeUtc,
        bool reachedStart,
        bool historyRequested,
        DateTimeOffset now)
    {
        NoteInteraction(now);
        return NavigatorCalculator.GoToTimeUtc(vs, mapper, timeUtc, reachedStart, historyRequested);
    }

    public void CenterOnBox(ViewState vs, int n, VisibleRangeBox box, double panelWidth, DateTimeOffset now)
    {
        NoteInteraction(now);
        NavigatorCalculator.CenterOnBox(vs, n, box, panelWidth);
    }

    public void RefreshOverview(IReadOnlyList<Candle> bars, DateTimeOffset now)
    {
        _model.ComputeOverview(bars);
        if (_model.IsVisible(now))
            NoteInteraction(now);
    }
}
