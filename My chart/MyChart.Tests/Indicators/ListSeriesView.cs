using MyChart.Core.Contracts.Data;
using MyChart.Core.Models.Market;

namespace MyChart.Tests.Indicators;

internal sealed class ListSeriesView : ISeriesView
{
    private readonly IReadOnlyList<Candle> _candles;

    public ListSeriesView(IReadOnlyList<Candle> candles) => _candles = candles;

    public int Count => _candles.Count;
    public bool HasForming => false;
    public Candle this[int index] => _candles[index];
    public DateTimeOffset OpenTime(int index) => _candles[index].Timestamp;
}
