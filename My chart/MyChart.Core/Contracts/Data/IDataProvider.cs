using MyChart.Core.Models.Market;

namespace MyChart.Core.Contracts.Data;

public interface IDataProvider
{
    Task ConnectAsync(CancellationToken cancellationToken);
    Task DisconnectAsync();
    Task<IReadOnlyList<Candle>> GetHistoryAsync(
        string symbol,
        Timeframe timeframe,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int maxBars,
        CancellationToken cancellationToken);
    IDisposable SubscribeTicks(string symbol, Action<Tick> onTick);
    void UnsubscribeTicks(string symbol);
    Task<IReadOnlyList<SymbolInfo>> GetSymbolsAsync(CancellationToken cancellationToken);
    IReadOnlyList<Timeframe> GetTimeframes();
    bool IsConnected { get; }
    string Name { get; }
    bool SupportsNativeHigherTimeframes { get; }
}
