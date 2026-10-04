using MyChart.Core.Models.Market;

namespace MyChart.Core.Analysis;

/// <summary>Min and Max of Close inside one pixel column of the navigator overview.</summary>
public readonly record struct OverviewColumn(double Min, double Max);

/// <summary>
/// T3.03 navigator overview (simplified) = min / max of Close per pixel column.
/// Column c covers the bars [c * N / columns, (c + 1) * N / columns) (integer division). With more columns
/// than bars each column shows the bar floor(c * N / columns). The result is a new array: the navigator
/// keeps its own cache, independent of the chart.
/// </summary>
public static class NavigatorOverview
{
    public static OverviewColumn[] Compute(IReadOnlyList<Candle> bars, int columns)
    {
        int n = bars.Count;
        if (n == 0 || columns <= 0) return Array.Empty<OverviewColumn>();

        var result = new OverviewColumn[columns];
        for (int c = 0; c < columns; c++)
        {
            int start = (int)((long)c * n / columns);
            int end = (int)((long)(c + 1) * n / columns);

            if (end <= start)
            {
                double single = bars[start].Close;
                result[c] = new OverviewColumn(single, single);
                continue;
            }

            double min = double.MaxValue;
            double max = double.MinValue;
            for (int i = start; i < end; i++)
            {
                double close = bars[i].Close;
                if (close < min) min = close;
                if (close > max) max = close;
            }

            result[c] = new OverviewColumn(min, max);
        }

        return result;
    }
}
