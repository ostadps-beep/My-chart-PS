using MyChart.Core.Models.Viewport;

namespace MyChart.Core.Scale;

/// <summary>
/// T2.02 CoordinateConverter — X(u), U(x), Y(price), Price(y).
/// X(u) = plotLeft + plotWidth - (N - 1 + RightOffset - u + 0.5) * BarSpacing
/// U(x) = N - 1 + RightOffset + 0.5 - (plotLeft + plotWidth - x) / BarSpacing
/// </summary>
public sealed class CoordinateConverter
{
    private readonly ViewState _vs;
    private int _n;

    public CoordinateConverter(ViewState viewState, int barCount = 0)
    {
        _vs = viewState;
        _n = barCount;
    }

    public int N
    {
        get => _n;
        set => _n = value;
    }

    public ViewState ViewState => _vs;

    public double X(double u)
    {
        // X(u) = plotLeft + plotWidth - (N - 1 + RightOffset - u + 0.5) * BarSpacing
        return _vs.PlotLeft + _vs.PlotWidth
               - (_n - 1 + _vs.RightOffset - u + 0.5) * _vs.BarSpacing;
    }

    public double U(double x)
    {
        // U(x) = N - 1 + RightOffset + 0.5 - (plotLeft + plotWidth - x) / BarSpacing
        return _n - 1 + _vs.RightOffset + 0.5
               - (_vs.PlotLeft + _vs.PlotWidth - x) / _vs.BarSpacing;
    }

    public double Y(double price)
    {
        var ps = _vs.PriceScale;
        double p = ps.Transform(price);
        double min = ps.Transform(ps.MinPrice);
        double max = ps.Transform(ps.MaxPrice);
        double range = max - min;
        if (range == 0) return _vs.PlotTop + _vs.PlotHeight * 0.5;
        return _vs.PlotTop + (max - p) / range * _vs.PlotHeight;
    }

    public double Price(double y)
    {
        var ps = _vs.PriceScale;
        double min = ps.Transform(ps.MinPrice);
        double max = ps.Transform(ps.MaxPrice);
        double range = max - min;
        if (range == 0) return ps.MinPrice;
        double p = max - (y - _vs.PlotTop) / _vs.PlotHeight * range;
        return ps.InverseTransform(p);
    }
}
