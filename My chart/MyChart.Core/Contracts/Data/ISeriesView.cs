using MyChart.Core.Models.Market;

namespace MyChart.Core.Contracts.Data;

/// <summary>
/// T1.11 / T3.05 series view. Forming candle is the LAST element when HasForming is true.
/// Lives in Core so Indicators (Core-only) can Calculate without referencing Data.
/// </summary>
public interface ISeriesView
{
    int Count { get; }
    Candle this[int index] { get; }
    bool HasForming { get; }
    DateTimeOffset OpenTime(int index);
}
