using System.Globalization;
using MyChart.Core.Contracts.Data;
using MyChart.Core.Models.Market;

namespace MyChart.Data.Providers;

/// <summary>
/// T1.10 CsvProvider — file name = SYMBOL_TF.csv;
/// header = timestamp_utc,open,high,low,close,volume; ISO-8601 UTC; '.' decimal; ascending.
/// Sidecar SYMBOL.info: Digits=5; Group=Forex
/// </summary>
public sealed class CsvProvider : IDataProvider
{
    private readonly string _root;
    private readonly Dictionary<string, List<Action<Tick>>> _tickSubs = new(StringComparer.OrdinalIgnoreCase);

    public CsvProvider(string rootDirectory)
    {
        _root = rootDirectory ?? throw new ArgumentNullException(nameof(rootDirectory));
    }

    public string Name => "CSV";
    public bool IsConnected { get; private set; }
    public bool SupportsNativeHigherTimeframes => false;
    public ServerTimeRule ServerTimeRule => ServerTimeRule.Utc;

    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        IsConnected = true;
        return Task.CompletedTask;
    }

    public Task DisconnectAsync()
    {
        IsConnected = false;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Candle>> GetHistoryAsync(
        string symbol,
        Timeframe timeframe,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int maxBars,
        CancellationToken cancellationToken)
    {
        var path = Path.Combine(_root, $"{symbol.ToUpperInvariant()}_{timeframe}.csv");
        if (!File.Exists(path))
            return Task.FromResult<IReadOnlyList<Candle>>(Array.Empty<Candle>());

        var lines = File.ReadAllLines(path);
        if (lines.Length == 0)
            throw new InvalidOperationException("SourceCorrupted: empty file");

        var header = lines[0].Trim().ToLowerInvariant();
        if (header != "timestamp_utc,open,high,low,close,volume")
            throw new InvalidOperationException("SourceCorrupted: wrong header");

        var list = new List<Candle>();
        for (int i = 1; i < lines.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            var parts = line.Split(',');
            if (parts.Length < 6)
                throw new InvalidOperationException($"SourceCorrupted: row {i + 1}");

            var ts = DateTimeOffset.Parse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            double o = double.Parse(parts[1], CultureInfo.InvariantCulture);
            double h = double.Parse(parts[2], CultureInfo.InvariantCulture);
            double l = double.Parse(parts[3], CultureInfo.InvariantCulture);
            double c = double.Parse(parts[4], CultureInfo.InvariantCulture);
            double v = double.Parse(parts[5], CultureInfo.InvariantCulture);

            if (from.HasValue && ts < from.Value) continue;
            if (to.HasValue && ts > to.Value) continue;

            list.Add(new Candle(ts, o, h, l, c, v));
        }

        if (maxBars > 0 && list.Count > maxBars)
            list = list.Skip(list.Count - maxBars).ToList();

        return Task.FromResult<IReadOnlyList<Candle>>(list);
    }

    public IDisposable SubscribeTicks(string symbol, Action<Tick> onTick)
    {
        var key = symbol.ToUpperInvariant();
        if (!_tickSubs.TryGetValue(key, out var list))
        {
            list = new List<Action<Tick>>();
            _tickSubs[key] = list;
        }
        list.Add(onTick);
        return new Unsub(() => list.Remove(onTick));
    }

    public void UnsubscribeTicks(string symbol)
    {
        _tickSubs.Remove(symbol.ToUpperInvariant());
    }

    public Task<IReadOnlyList<SymbolInfo>> GetSymbolsAsync(CancellationToken cancellationToken)
    {
        var result = new List<SymbolInfo>();
        if (!Directory.Exists(_root))
            return Task.FromResult<IReadOnlyList<SymbolInfo>>(result);

        foreach (var infoPath in Directory.GetFiles(_root, "*.info"))
        {
            var name = Path.GetFileNameWithoutExtension(infoPath).ToUpperInvariant();
            int digits = 5;
            var group = SymbolGroup.Forex;
            foreach (var line in File.ReadAllLines(infoPath))
            {
                var p = line.Split('=', 2);
                if (p.Length != 2) continue;
                if (p[0].Trim().Equals("Digits", StringComparison.OrdinalIgnoreCase))
                    int.TryParse(p[1].Trim(), out digits);
                if (p[0].Trim().Equals("Group", StringComparison.OrdinalIgnoreCase)
                    && Enum.TryParse<SymbolGroup>(p[1].Trim(), true, out var g))
                    group = g;
            }
            result.Add(new SymbolInfo(name, name, group, digits));
        }
        return Task.FromResult<IReadOnlyList<SymbolInfo>>(result);
    }

    public IReadOnlyList<Timeframe> GetTimeframes()
        => new[] { Timeframe.M1, Timeframe.M5, Timeframe.M15, Timeframe.M30, Timeframe.H1, Timeframe.H4, Timeframe.D1, Timeframe.W1, Timeframe.MN1 };

    private sealed class Unsub(Action a) : IDisposable
    {
        private Action? _a = a;
        public void Dispose() => Interlocked.Exchange(ref _a, null)?.Invoke();
    }
}
