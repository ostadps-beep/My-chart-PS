using MyChart.Core.Models.Geometry;

namespace MyChart.Core.Contracts.Plugins;

public interface IChartMapper
{
    double X(double u);
    double U(double x);
    double Y(double price);
    double Price(double y);
    double IndexOfTime(DateTimeOffset t);
    DateTimeOffset TimeAtIndex(double u);
    int SnapIndex(double u);
    RectD PlotRect { get; }
}
