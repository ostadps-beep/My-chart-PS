using MyChart.Core.Models.Market;

namespace MyChart.Core.Contracts.Data;

public interface IDataStorage
{
    Task SaveCandlesAsync(string symbol, Timeframe timeframe, IReadOnlyList<Candle> candles, CancellationToken cancellationToken);
    Task<IReadOnlyList<Candle>> LoadCandlesAsync(string symbol, Timeframe timeframe, CancellationToken cancellationToken);
}
