using MyChart.Core.Analysis;
using MyChart.Core.Candles;
using MyChart.Core.Contracts.Data;
using MyChart.Core.Models.Chart;
using MyChart.Core.Models.Market;

namespace MyChart.Data.Offline;

/// <summary>
/// T5.06 — when the provider is disconnected: no ticks; series from MemoryCache then IDataStorage.
/// Chart navigation remains available (ViewState is independent of connection).
/// </summary>
public sealed class OfflineSeriesSource
{
    private readonly MemoryCache _cache;
    private readonly IDataStorage? _storage;

    public OfflineSeriesSource(MemoryCache cache, IDataStorage? storage = null)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _storage = storage;
    }

    /// <summary>
    /// Resolve series: cache first, then database. Empty list if neither has data.
    /// </summary>
    public async Task<IReadOnlyList<Candle>> GetSeriesAsync(
        string symbol,
        Timeframe timeframe,
        CancellationToken cancellationToken = default)
    {
        var cached = _cache.Get(symbol, timeframe);
        if (cached is { Count: > 0 })
            return cached;

        if (_storage is null)
            return Array.Empty<Candle>();

        var fromDb = await _storage.LoadCandlesAsync(symbol, timeframe, cancellationToken).ConfigureAwait(false);
        if (fromDb is { Count: > 0 })
        {
            _cache.Set(symbol, timeframe, fromDb);
            return fromDb;
        }

        return Array.Empty<Candle>();
    }
}

/// <summary>
/// T5.06 coordinator: tracks connection, HUD status, tick gate, and reconnect gap repair.
/// </summary>
public sealed class OfflineModeController
{
    private readonly MemoryCache _cache;
    private readonly OfflineSeriesSource _series;
    private bool _providerConnected = true;
    private bool _replayActive;
    private Tick? _lastTick;
    private DateTimeOffset? _disconnectedAt;

    public OfflineModeController(MemoryCache cache, IDataStorage? storage = null)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _series = new OfflineSeriesSource(cache, storage);
    }

    public bool ProviderConnected => _providerConnected;
    public bool AcceptsTicks => _providerConnected && !_replayActive;
    public DateTimeOffset? DisconnectedAt => _disconnectedAt;

    public void SetReplayActive(bool active) => _replayActive = active;

    public void NotifyProviderConnected()
    {
        _providerConnected = true;
        _disconnectedAt = null;
    }

    public void NotifyProviderDisconnected(DateTimeOffset? at = null)
    {
        _providerConnected = false;
        _disconnectedAt = at ?? DateTimeOffset.UtcNow;
    }

    public void OnTickAccepted(Tick tick)
    {
        if (!AcceptsTicks)
            return;
        _lastTick = tick;
    }

    public HudMarketStatus MarketStatus(DateTimeOffset serverNow)
        => HudDataProvider.MarketStatusOf(_replayActive, _providerConnected, _lastTick, serverNow);

    public Task<IReadOnlyList<Candle>> GetSeriesAsync(string symbol, Timeframe tf, CancellationToken ct = default)
        => _series.GetSeriesAsync(symbol, tf, ct);

    /// <summary>
    /// After reconnect: merge incoming history for the outage window and detect remaining gaps.
    /// Returns merged series and remaining Missing gaps (caller may retry up to MaxRepairAttempts).
    /// </summary>
    public (IReadOnlyList<Candle> Series, IReadOnlyList<GapInfo> RemainingGaps) RepairAfterReconnect(
        string symbol,
        Timeframe timeframe,
        IReadOnlyList<Candle> existing,
        IReadOnlyList<Candle> incomingRepair,
        SessionCalendar calendar)
    {
        var (merged, remaining) = HistoryMerge.TryRepairMissing(existing, incomingRepair, timeframe, calendar);
        _cache.Set(symbol, timeframe, merged);
        NotifyProviderConnected();
        return (merged, remaining);
    }

    /// <summary>
    /// View navigation does not require a live provider — always true while chart is open.
    /// </summary>
    public bool ChartNavigable => true;
}
