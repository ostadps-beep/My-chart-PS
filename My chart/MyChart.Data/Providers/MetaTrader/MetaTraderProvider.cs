using System.Text;
using MyChart.Core.Contracts.Data;
using MyChart.Core.Models.Market;

namespace MyChart.Data.Providers.MetaTrader;

/// <summary>
/// T5.03 MetaTrader provider over a bidirectional text stream (pipe/TCP abstracted as Stream).
/// Convert server time to UTC using HELLO offset; ServerTimeRule = Fixed(offset).
/// </summary>
public sealed class MetaTraderProvider : IDataProvider
{
    private readonly Stream _stream;
    private readonly StreamReader _reader;
    private readonly StreamWriter _writer;
    private readonly Dictionary<string, List<Action<Tick>>> _tickSubs = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<SymbolInfo> _symbols = new();
    private readonly object _gate = new();
    private CancellationTokenSource? _readCts;
    private Task? _readLoop;
    private TimeSpan _serverOffset = TimeSpan.Zero;
    private readonly List<Candle> _pendingHistory = new();
    private TaskCompletionSource<IReadOnlyList<Candle>>? _historyTcs;

    public MetaTraderProvider(Stream stream, string name = "MetaTrader")
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        _reader = new StreamReader(_stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
        _writer = new StreamWriter(_stream, Encoding.UTF8, bufferSize: 1024, leaveOpen: true) { AutoFlush = true };
        Name = name;
    }

    public string Name { get; }
    public bool IsConnected { get; private set; }
    public bool SupportsNativeHigherTimeframes => false;
    public ServerTimeRule ServerTimeRule { get; private set; } = ServerTimeRule.Utc;

    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        IsConnected = true;
        _readCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _readLoop = Task.Run(() => ReadLoopAsync(_readCts.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    public async Task DisconnectAsync()
    {
        IsConnected = false;
        if (_readCts is not null)
        {
            _readCts.Cancel();
            try { if (_readLoop is not null) await _readLoop.ConfigureAwait(false); } catch { /* ignore */ }
            _readCts.Dispose();
            _readCts = null;
        }
    }

    public async Task<IReadOnlyList<Candle>> GetHistoryAsync(
        string symbol,
        Timeframe timeframe,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int maxBars,
        CancellationToken cancellationToken)
    {
        var fromSec = from?.ToUnixTimeSeconds() ?? 0;
        var toSec = to?.ToUnixTimeSeconds() ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var tcs = new TaskCompletionSource<IReadOnlyList<Candle>>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_gate)
        {
            _pendingHistory.Clear();
            _historyTcs = tcs;
        }

        await WriteAsync(new MtMessage.GetHistory(symbol, timeframe.ToString(), fromSec, toSec, maxBars), cancellationToken)
            .ConfigureAwait(false);

        using var reg = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
        return await tcs.Task.ConfigureAwait(false);
    }

    public IDisposable SubscribeTicks(string symbol, Action<Tick> onTick)
    {
        lock (_gate)
        {
            if (!_tickSubs.TryGetValue(symbol, out var list))
            {
                list = new List<Action<Tick>>();
                _tickSubs[symbol] = list;
            }
            list.Add(onTick);
        }
        return new Unsub(() =>
        {
            lock (_gate)
            {
                if (_tickSubs.TryGetValue(symbol, out var list))
                    list.Remove(onTick);
            }
        });
    }

    public void UnsubscribeTicks(string symbol)
    {
        lock (_gate) _tickSubs.Remove(symbol);
    }

    public Task<IReadOnlyList<SymbolInfo>> GetSymbolsAsync(CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<SymbolInfo>>(_symbols.ToList());

    public IReadOnlyList<Timeframe> GetTimeframes() => new[] { Timeframe.M1 };

    public async Task WriteAsync(MtMessage message, CancellationToken ct = default)
    {
        var line = MessageCodec.Encode(message);
        await _writer.WriteLineAsync(line.AsMemory(), ct).ConfigureAwait(false);
    }

    /// <summary>Inject a line as if received from the EA (tests / alternate transports).</summary>
    public void HandleLine(string line) => Dispatch(MessageCodec.Decode(line));

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var line = await _reader.ReadLineAsync(ct).ConfigureAwait(false);
                if (line is null) break;
                Dispatch(MessageCodec.Decode(line));
            }
        }
        catch (OperationCanceledException) { /* normal */ }
        catch (IOException) { IsConnected = false; }
    }

    private void Dispatch(MtMessage msg)
    {
        switch (msg)
        {
            case MtMessage.Hello h:
                _serverOffset = TimeSpan.FromMinutes(h.ServerUtcOffsetMinutes);
                ServerTimeRule = new ServerTimeRule.Fixed(_serverOffset);
                break;
            case MtMessage.SymbolInfo s:
                var group = ParseGroup(s.GroupHint);
                _symbols.Add(new SymbolInfo(s.Name, s.Name, group, s.Digits));
                break;
            case MtMessage.Tick t:
                var utc = DateTimeOffset.FromUnixTimeMilliseconds(t.ServerTimeMs) - _serverOffset;
                // If serverTimeMs is already UTC with offset applied by EA as server clock,
                // convert: UTC = serverTime - offset (offset = server - UTC).
                var tick = new Tick(utc, t.Bid, t.Ask, t.Volume);
                List<Action<Tick>>? handlers = null;
                lock (_gate)
                {
                    if (_tickSubs.TryGetValue(t.Symbol, out var list))
                        handlers = list.ToList();
                }
                if (handlers is not null)
                    foreach (var hnd in handlers) hnd(tick);
                break;
            case MtMessage.HistoryBar b:
                var barUtc = DateTimeOffset.FromUnixTimeSeconds(b.ServerTimeSec) - _serverOffset;
                lock (_gate)
                    _pendingHistory.Add(new Candle(barUtc, b.O, b.H, b.L, b.C, b.Vol));
                break;
            case MtMessage.HistoryEnd:
                lock (_gate)
                {
                    _historyTcs?.TrySetResult(_pendingHistory.ToList());
                    _historyTcs = null;
                }
                break;
            case MtMessage.Ping:
                _ = WriteAsync(new MtMessage.Pong());
                break;
        }
    }

    private static SymbolGroup ParseGroup(string hint) => hint.ToUpperInvariant() switch
    {
        "FOREX" => SymbolGroup.Forex,
        "CRYPTO" => SymbolGroup.Crypto,
        "INDICES" or "INDEX" => SymbolGroup.Indices,
        "STOCKS" or "STOCK" => SymbolGroup.Stocks,
        "COMMODITIES" or "COMMODITY" => SymbolGroup.Commodities,
        _ => SymbolGroup.Forex
    };

    private sealed class Unsub(Action action) : IDisposable
    {
        public void Dispose() => action();
    }
}
