using MyChart.Core.Contracts.Plugins;
using MyChart.Core.Models.Geometry;
using MyChart.Core.Models.Market;
using MyChart.Core.Models.Viewport;
using MyChart.Core.Scale;

namespace MyChart.Tests.Support;

/// <summary>
/// Test adapter: IChartMapper over a ViewState and a TimeIndexMapper (the same formulas the chart uses).
/// Standard() builds the golden-vector view: N 100 M15 bars from 2024-03-13T12:00Z, plotLeft 0, plotWidth 800,
/// BarSpacing 8, RightOffset 5, price range 1.09400..1.10600, plotHeight 500, so X(i) = 8 * i - 36 and
/// Y(price) = (1.1060 - price) / 0.012 * 500.
/// </summary>
public sealed class ViewChartMapper : IChartMapper
{
    public static readonly DateTimeOffset FirstOpen = new(2024, 3, 13, 12, 0, 0, TimeSpan.Zero);

    private readonly CoordinateConverter _converter;
    private readonly TimeIndexMapper _time;

    public ViewChartMapper(ViewState viewState, TimeIndexMapper time)
    {
        ViewState = viewState;
        _time = time;
        _converter = new CoordinateConverter(viewState, time.N);
    }

    public ViewState ViewState { get; }

    public static ViewChartMapper Standard(int n = 100)
    {
        var vs = new ViewState
        {
            Width = 864,
            Height = 524,
            PriceAxisWidth = 64,
            TimeAxisHeight = 24,
            BarSpacing = 8,
            RightOffset = 5
        };
        vs.PriceScale.MinPrice = 1.0940;
        vs.PriceScale.MaxPrice = 1.1060;

        var opens = new List<DateTimeOffset>();
        for (int i = 0; i < n; i++)
            opens.Add(FirstOpen.AddMinutes(15 * i));

        var calendar = new SessionCalendar(ServerTimeRule.Utc, SymbolGroup.Forex);
        return new ViewChartMapper(vs, new TimeIndexMapper(opens, Timeframe.M15, calendar));
    }

    public double X(double u) => _converter.X(u);

    public double U(double x) => _converter.U(x);

    public double Y(double price) => _converter.Y(price);

    public double Price(double y) => _converter.Price(y);

    public double IndexOfTime(DateTimeOffset t) => _time.IndexOfTime(t);

    public DateTimeOffset TimeAtIndex(double u) => _time.TimeAtIndex(u);

    public int SnapIndex(double u) => _time.SnapIndex(u);

    public RectD PlotRect => new(ViewState.PlotLeft, ViewState.PlotTop, ViewState.PlotWidth, ViewState.PlotHeight);
}
