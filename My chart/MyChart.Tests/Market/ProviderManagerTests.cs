using MyChart.Core.Contracts.Data;
using MyChart.Core.Models.Market;
using MyChart.Data.Providers;
using Xunit;

namespace MyChart.Tests.Market;

public class ProviderManagerTests
{
    private sealed class FakeProvider : IDataProvider
    {
        public string Name { get; }
        public bool IsConnected { get; private set; }
        public bool FailConnect { get; set; }
        public int ConnectCalls { get; private set; }
        public int DisconnectCalls { get; private set; }
        public bool SupportsNativeHigherTimeframes => false;
        public ServerTimeRule ServerTimeRule => ServerTimeRule.Utc;

        public FakeProvider(string name) => Name = name;

        public Task ConnectAsync(CancellationToken cancellationToken)
        {
            ConnectCalls++;
            if (FailConnect) throw new InvalidOperationException("connect failed");
            IsConnected = true;
            return Task.CompletedTask;
        }

        public Task DisconnectAsync()
        {
            DisconnectCalls++;
            IsConnected = false;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Candle>> GetHistoryAsync(string symbol, Timeframe timeframe, DateTimeOffset? from, DateTimeOffset? to, int maxBars, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Candle>>(Array.Empty<Candle>());

        public IDisposable SubscribeTicks(string symbol, Action<Tick> onTick) => new Dummy();
        public void UnsubscribeTicks(string symbol) { }
        public Task<IReadOnlyList<SymbolInfo>> GetSymbolsAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<SymbolInfo>>(Array.Empty<SymbolInfo>());
        public IReadOnlyList<Timeframe> GetTimeframes() => new[] { Timeframe.M1 };

        private sealed class Dummy : IDisposable { public void Dispose() { } }
    }

    [Fact]
    public async Task Switch_DisconnectsOld_ConnectsNew_RaisesProviderChanged()
    {
        var mgr = new ProviderManager();
        var a = new FakeProvider("A");
        var b = new FakeProvider("B");
        mgr.Register(a);
        mgr.Register(b);

        string? from = null, to = null;
        mgr.ProviderChanged += (f, t) => { from = f; to = t; };

        await mgr.SwitchAsync("A");
        Assert.Equal(ProviderConnectionState.Connected, mgr.State);
        Assert.True(a.IsConnected);

        await mgr.SwitchAsync("B");
        Assert.Equal(1, a.DisconnectCalls);
        Assert.True(b.IsConnected);
        Assert.Equal("A", from);
        Assert.Equal("B", to);
        Assert.Equal("B", mgr.ActiveName);
    }

    [Fact]
    public async Task Health_Timeout_MarksDisconnected_AndBackoffSequence()
    {
        var now = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var mgr = new ProviderManager { UtcNow = () => now };
        var p = new FakeProvider("P");
        mgr.Register(p);
        await mgr.SwitchAsync("P");
        mgr.NoteHeartbeat();

        // still healthy
        now = now.AddSeconds(10);
        Assert.Equal(0, mgr.CheckHealth());
        Assert.Equal(ProviderConnectionState.Connected, mgr.State);

        // past 15 s
        now = now.AddSeconds(10);
        int d1 = mgr.CheckHealth();
        Assert.Equal(ProviderConnectionState.Disconnected, mgr.State);
        Assert.Equal(1, d1);

        int d2 = mgr.NextBackoffSeconds();
        int d3 = mgr.NextBackoffSeconds();
        int d4 = mgr.NextBackoffSeconds();
        int d5 = mgr.NextBackoffSeconds();
        int d6 = mgr.NextBackoffSeconds();
        int d7 = mgr.NextBackoffSeconds();
        Assert.Equal(new[] { 2, 4, 8, 16, 30, 30 }, new[] { d2, d3, d4, d5, d6, d7 });
    }

    [Fact]
    public async Task Reconnect_Failure_Increments_AndFailoverFlag()
    {
        var mgr = new ProviderManager();
        var primary = new FakeProvider("Primary") { FailConnect = true };
        var secondary = new FakeProvider("Secondary");
        mgr.Register(primary);
        mgr.Register(secondary);
        mgr.SetSecondary("Secondary");

        // Force active without successful connect path for reconnect tests
        primary.FailConnect = false;
        await mgr.SwitchAsync("Primary");
        primary.FailConnect = true;

        Assert.False(await mgr.TryReconnectAsync());
        Assert.Equal(1, mgr.FailedReconnects);
        Assert.False(mgr.ShouldFailover());

        Assert.False(await mgr.TryReconnectAsync());
        Assert.False(await mgr.TryReconnectAsync());
        Assert.Equal(3, mgr.FailedReconnects);
        Assert.True(mgr.ShouldFailover());
    }

    [Fact]
    public async Task Reconnect_Success_ResetsCounters()
    {
        var mgr = new ProviderManager();
        var p = new FakeProvider("P");
        mgr.Register(p);
        await mgr.SwitchAsync("P");
        p.FailConnect = true;
        await mgr.TryReconnectAsync();
        Assert.Equal(1, mgr.FailedReconnects);
        p.FailConnect = false;
        Assert.True(await mgr.TryReconnectAsync());
        Assert.Equal(0, mgr.FailedReconnects);
        Assert.Equal(ProviderConnectionState.Connected, mgr.State);
    }
}
