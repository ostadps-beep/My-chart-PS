using MyChart.Core.Models.Market;

namespace MyChart.Core.Contracts.Data;

public interface IAggregationEngine
{
    void SetBaseHistory(string symbol, IReadOnlyList<Candle> candles);
    void OnTick(string symbol, Tick tick);
    // ISeriesView returned later when defined in T1.11
}
