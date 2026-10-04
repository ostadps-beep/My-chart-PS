using MyChart.Core.Models.Market;

namespace MyChart.Data.Aggregation;

/// <summary>T1.11 ISeriesView — forming candle is LAST element when HasForming.</summary>
public interface ISeriesView
{
    int Count { get; }
    Candle this[int index] { get; }
    bool HasForming { get; }
    DateTimeOffset OpenTime(int index);
}
