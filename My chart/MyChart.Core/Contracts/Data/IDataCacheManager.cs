using MyChart.Core.Models.Market;

namespace MyChart.Core.Contracts.Data;

public interface IDataCacheManager
{
    IReadOnlyList<Candle>? Get(string symbol, Timeframe timeframe);
    void Set(string symbol, Timeframe timeframe, IReadOnlyList<Candle> candles);
}
