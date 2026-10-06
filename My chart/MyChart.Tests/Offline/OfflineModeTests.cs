using MyChart.Core.Candles;
using MyChart.Core.Contracts.Data;
using MyChart.Core.Models.Chart;
using MyChart.Core.Models.Market;
using MyChart.Data.Offline;
using Xunit;

namespace MyChart.Tests.Offline;

/// <summary>
/// T5.06 VERIFY — disconnect while chart open: navigable; series from cache/db; no ticks;
/// reconnect resumes and runs gap repair.
/// </summary>
public class OfflineModeTests
{
    private static DateTimeOffset T(int hour, int minute = 0)
        => new(2024, 3, 13, hour, minute, 0, TimeSpan.Zero);

    private static SessionCalendar ForexUtc => new(ServerTimeRule.Utc, SymbolGroup.Forex);

    private static Candle Bar(int h, int m, double px = 1.1)
        => new(T(h, m), px, px, px, px, 1);

    private sealed class FakeStorage : IDataStorage
    {
        public List<Candle> Stored { get; set; } = new();

        public Task SaveCandlesAsync(string symbol, Timeframe timeframe, IReadOnlyList<Candle> candles, CancellationToken cancellationToken)
        {
            Stored = candles.ToList();
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Candle>> LoadCandlesAsync(string symbol, Timeframe timeframe, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Candle>>(Stored);
    }

    [Fact]
    public void Disconnect_Sets_Offline_And_Blocks_Ticks()
    {
        var ctrl = new OfflineModeController(new MemoryCache());
        Assert.True(ctrl.AcceptsTicks);

        ctrl.NotifyProviderDisconnected(T(12, 0));
        Assert.False(ctrl.ProviderConnected);
        Assert.False(ctrl.AcceptsTicks);
        Assert.Equal(HudMarketStatus.Offline, ctrl.MarketStatus(T(12, 5)));

        ctrl.OnTickAccepted(new Tick(T(12, 1), 1.1, 1.1001, 1));
        Assert.Equal(HudMarketStatus.Offline, ctrl.MarketStatus(T(12, 5)));
    }

    [Fact]
    public void Chart_Remains_Navigable_While_Offline()
    {
        var ctrl = new OfflineModeController(new MemoryCache());
        ctrl.NotifyProviderDisconnected();
        Assert.True(ctrl.ChartNavigable);
    }

    [Fact]
    public async Task Series_From_Cache_When_Offline()
    {
        var cache = new MemoryCache();
        var bars = new[] { Bar(12, 0), Bar(12, 1), Bar(12, 2) };
        cache.Set("EURUSD", Timeframe.M1, bars);

        var ctrl = new OfflineModeController(cache);
        ctrl.NotifyProviderDisconnected();

        var series = await ctrl.GetSeriesAsync("EURUSD", Timeframe.M1);
        Assert.Equal(3, series.Count);
        Assert.Equal(T(12, 0), series[0].Timestamp);
    }

    [Fact]
    public async Task Series_Falls_Back_To_Database()
    {
        var storage = new FakeStorage
        {
            Stored = { Bar(10, 0), Bar(10, 1) }
        };
        var ctrl = new OfflineModeController(new MemoryCache(), storage);
        ctrl.NotifyProviderDisconnected();

        var series = await ctrl.GetSeriesAsync("EURUSD", Timeframe.M1);
        Assert.Equal(2, series.Count);
        Assert.Equal(2, (await ctrl.GetSeriesAsync("EURUSD", Timeframe.M1)).Count);
    }

    [Fact]
    public void Reconnect_Resumes_Ticks_And_Repairs_Gaps()
    {
        var cache = new MemoryCache();
        var existing = new[] { Bar(12, 0), Bar(12, 2) };
        cache.Set("EURUSD", Timeframe.M1, existing);

        var ctrl = new OfflineModeController(cache);
        ctrl.NotifyProviderDisconnected(T(12, 5));

        var repair = new[] { Bar(12, 1, 1.105) };
        var (merged, remaining) = ctrl.RepairAfterReconnect(
            "EURUSD", Timeframe.M1, existing, repair, ForexUtc);

        Assert.True(ctrl.ProviderConnected);
        Assert.True(ctrl.AcceptsTicks);
        Assert.Equal(3, merged.Count);
        Assert.Equal(T(12, 1), merged[1].Timestamp);
        Assert.Empty(remaining);

        ctrl.OnTickAccepted(new Tick(T(12, 10), 1.1, 1.1001, 1));
        Assert.Equal(HudMarketStatus.Live, ctrl.MarketStatus(T(12, 10)));
    }

    [Fact]
    public void Replay_Overrides_Offline_For_Status()
    {
        var ctrl = new OfflineModeController(new MemoryCache());
        ctrl.NotifyProviderDisconnected();
        ctrl.SetReplayActive(true);
        Assert.Equal(HudMarketStatus.Replay, ctrl.MarketStatus(T(12, 0)));
        Assert.False(ctrl.AcceptsTicks);
    }
}
