using MyChart.Core.Contracts.Data;
using MyChart.Core.Models.Market;

namespace MyChart.Infrastructure.Providers.MetaTrader;

/// <summary>
/// T5.03 MetaTrader data provider. Transport is injected (pipe/TCP/in-memory).
/// Server times are converted to UTC using the offset from HELLO.
/// </summary>
public sealed class MetaTraderProvider : IDataProvider, IAsyncDisposable
{
    private readonly IMtTransport _transport;
    private readonly object _gate = new();
    private readonly List<SymbolInfo> _symbols = new();
    private readonly Dictionary<string, List<Action<Tick>>> _tickSubs =
        new(StringComparer.OrdinalIgnoreCase);

    private TimeSpan _serverOffset = TimeSpan.Zero;
    private ServerTimeRule _timeRule = ServerTimeRule.Utc;
    private bool _useEetUsDst;
    private TaskCompletionSource<IReadOnlyList<Candle>>? _historyWaiter;
    private readonly List<Candle> _historyBuffer = new();
    private string? _historySymbol;
    private CancellationTokenSource? _pingCts;
    private Task? _pingLoop;

    public MetaTraderProvider(IMtTransport transport, bool useEetUsDst = false)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _useEetUsDst = useEetUsDst;
        _transport.LineReceived += OnLine;
    }

    public string Name => "MetaTrader";
    public bool IsConnected { get; private set; }
    public bool SupportsNativeHigherTimeframes => false;
    public ServerTimeRule ServerTimeRule => _timeRule;
    public string? EaVersion { get; private set; }

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        await _transport.ConnectAsync(cancellationToken).ConfigureAwait(false);
        IsConnected = true;
        _pingCts = new CancellationTokenSource();
        _pingLoop = RunPingLoopAsync(_pingCts.Token);
    }

    public async Task DisconnectAsync()
    {
        if (_pingCts is not null)
        {
            _pingCts.Cancel();
            try { if (_pingLoop is not null) await _pingLoop.ConfigureAwait(false); } catch { /* ignore */ }
            _pingCts.Dispose();
            _pingCts = null;
        }
        await _transport.DisconnectAsync().ConfigureAwait(false);
        IsConnected = false;
    }

    public async Task<IReadOnlyList<Candle>> GetHistoryAsync(
        string symbol,
        Timeframe timeframe,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int maxBars,
        CancellationToken cancellationToken)
    {
        if (timeframe != Timeframe.M1)
            return Array.Empty<Candle>();
        if (!IsConnected)
            return Array.Empty<Candle>();

        var fromUtc = from ?? DateTimeOffset.UtcNow.AddDays(-7);
        var toUtc = to ?? DateTimeOffset.UtcNow;
        var tcs = new TaskCompletionSource<IReadOnlyList<Candle>>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_gate)
        {
            _historyBuffer.Clear();
            _historySymbol = symbol;
            _historyWaiter = tcs;
        }

        var line = MessageCodec.EncodeGetHistory(symbol, fromUtc, toUtc, maxBars);
        await _transport.SendLineAsync(line, cancellationToken).ConfigureAwait(false);

        using var reg = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        return await tcs.Task.ConfigureAwait(false);
    }

    public IDisposable SubscribeTicks(string symbol, Action<Tick> onTick)
    {
        ArgumentNullException.ThrowIfNull(onTick);
        lock (_gate)
        {
            if (!_tickSubs.TryGetValue(symbol, out var list))
            {
                list = new List<Action<Tick>>();
                _tickSubs[symbol] = list;
            }
            list.Add(onTick);
        }
        return new Unsub(this, symbol, onTick);
    }

    public void UnsubscribeTicks(string symbol)
    {
        lock (_gate)
            _tickSubs.Remove(symbol);
    }

    public Task<IReadOnlyList<SymbolInfo>> GetSymbolsAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
            return Task.FromResult<IReadOnlyList<SymbolInfo>>(_symbols.ToList());
    }

    public IReadOnlyList<Timeframe> GetTimeframes() =>
        new[] { Timeframe.M1, Timeframe.M5, Timeframe.M15, Timeframe.M30, Timeframe.H1, Timeframe.H4, Timeframe.D1, Timeframe.W1, Timeframe.MN1 };

    private void OnLine(string line)
    {
        if (!MessageCodec.TryParse(line, out var msg))
            return;

        switch (msg)
        {
            case MtMessage.Hello hello:
                EaVersion = hello.EaVersion;
                _serverOffset = TimeSpan.FromMinutes(hello.ServerUtcOffsetMinutes);
                _timeRule = _useEetUsDst
                    ? new ServerTimeRule.EetUsDst()
                    : new ServerTimeRule.Fixed(_serverOffset);
                break;

            case MtMessage.Symbol sym:
                var info = new SymbolInfo(
                    sym.Name,
                    Name,
                    MessageCodec.ParseGroupHint(sym.GroupHint),
                    sym.Digits);
                lock (_gate)
                {
                    _symbols.RemoveAll(s => s.Name.Equals(sym.Name, StringComparison.OrdinalIgnoreCase));
                    _symbols.Add(info);
                }
                break;

            case MtMessage.TickMsg t:
                var utc = MessageCodec.ServerMsToUtc(t.ServerTimeMs, _serverOffset);
                var tick = new Tick(utc, t.Bid, t.Ask, t.Volume);
                List<Action<Tick>>? handlers = null;
                lock (_gate)
                {
                    if (_tickSubs.TryGetValue(t.Symbol, out var list))
                        handlers = list.ToList();
                }
                if (handlers is not null)
                {
                    foreach (var h in handlers)
                        h(tick);
                }
                break;

            case MtMessage.HistoryBar bar:
                var barUtc = MessageCodec.ServerSecToUtc(bar.ServerTimeSec, _serverOffset);
                var candle = new Candle(barUtc, bar.O, bar.H, bar.L, bar.C, bar.Vol);
                lock (_gate)
                {
                    if (_historySymbol is not null
                        && bar.Symbol.Equals(_historySymbol, StringComparison.OrdinalIgnoreCase))
                        _historyBuffer.Add(candle);
                }
                break;

            case MtMessage.HistoryEnd hend:
                TaskCompletionSource<IReadOnlyList<Candle>>? waiter = null;
                List<Candle> snapshot;
                lock (_gate)
                {
                    snapshot = _historyBuffer.ToList();
                    waiter = _historyWaiter;
                    _historyWaiter = null;
                    _historySymbol = null;
                    _historyBuffer.Clear();
                }
                waiter?.TrySetResult(snapshot);
                break;

            case MtMessage.PingMsg:
                _ = _transport.SendLineAsync(MessageCodec.EncodePong(), CancellationToken.None);
                break;

            case MtMessage.PongMsg:
                break;
        }
    }

    private async Task RunPingLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), ct).ConfigureAwait(false);
                if (IsConnected)
                    await _transport.SendLineAsync(MessageCodec.EncodePing(), ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // keep loop alive; health is owned by T5.02 ProviderManager
            }
        }
    }

    public async ValueTask DisposeAsync() => await DisconnectAsync().ConfigureAwait(false);

    private sealed class Unsub : IDisposable
    {
        private readonly MetaTraderProvider _owner;
        private readonly string _symbol;
        private readonly Action<Tick> _handler;
        private int _done;

        public Unsub(MetaTraderProvider owner, string symbol, Action<Tick> handler)
        {
            _owner = owner;
            _symbol = symbol;
            _handler = handler;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _done, 1) == 1)
                return;
            lock (_owner._gate)
            {
                if (_owner._tickSubs.TryGetValue(_symbol, out var list))
                {
                    list.Remove(_handler);
                    if (list.Count == 0)
                        _owner._tickSubs.Remove(_symbol);
                }
            }
        }
    }
}
