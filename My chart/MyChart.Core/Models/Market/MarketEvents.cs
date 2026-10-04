namespace MyChart.Core.Models.Market;

/// <summary>T1.07 MarketDataBus event payloads.</summary>
public sealed record TickReceived(string Symbol, Tick Tick);

public sealed record CandleUpdated(string Symbol, Timeframe Timeframe, Candle Forming);

public sealed record CandleClosed(string Symbol, Timeframe Timeframe, Candle Candle);

public sealed record NewSymbol(SymbolInfo Symbol);

public sealed record NewTimeframe(string Symbol, Timeframe Timeframe);

public sealed record HistoryLoaded(
    string Symbol,
    Timeframe Timeframe,
    int Count,
    DateTimeOffset FirstTimestamp,
    DateTimeOffset LastTimestamp);

public enum ProviderState
{
    Connected,
    Disconnected,
    Reconnecting,
    Error
}

public sealed record ProviderChanged(string ProviderName, ProviderState State);
