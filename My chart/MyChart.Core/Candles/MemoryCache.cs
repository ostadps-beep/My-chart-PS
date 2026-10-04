using MyChart.Core.Models.Market;

namespace MyChart.Core.Candles;

/// <summary>
/// T1.09 MemoryCache — KEY=(Symbol, Timeframe).
/// LIMITS: 8 symbols in memory (LRU eviction); M1 series capped at 600000 bars per symbol.
/// Derived timeframes rebuilt from M1 on demand and dropped with the symbol.
/// </summary>
public sealed class MemoryCache
{
    public const int MaxSymbols = 8;
    public const int M1CapBars = 600_000;

    private readonly Dictionary<string, SymbolEntry> _symbols = new(StringComparer.Ordinal);
    private readonly LinkedList<string> _lru = new();

    private sealed class SymbolEntry
    {
        public LinkedListNode<string>? LruNode;
        public readonly Dictionary<Timeframe, List<Candle>> Series = new();
    }

    public int SymbolCount => _symbols.Count;

    public void Set(string symbol, Timeframe timeframe, IReadOnlyList<Candle> candles)
    {
        var key = SymbolRegistry.Normalize(symbol);
        Touch(key);

        var list = candles.OrderBy(c => c.Timestamp).ToList();
        if (timeframe == Timeframe.M1 && list.Count > M1CapBars)
            list = list.Skip(list.Count - M1CapBars).ToList();

        _symbols[key].Series[timeframe] = list;
    }

    public IReadOnlyList<Candle>? Get(string symbol, Timeframe timeframe)
    {
        var key = SymbolRegistry.Normalize(symbol);
        if (!_symbols.TryGetValue(key, out var entry))
            return null;

        TouchExisting(key, entry);

        if (entry.Series.TryGetValue(timeframe, out var series))
            return series;

        // Derived timeframes rebuilt from M1 on demand
        if (timeframe != Timeframe.M1
            && entry.Series.TryGetValue(Timeframe.M1, out var m1)
            && m1.Count > 0)
        {
            var cal = new SessionCalendar(ServerTimeRule.Utc, SymbolGroup.Forex);
            var derived = CandleFold.Aggregate(m1, Timeframe.M1, timeframe, cal);
            entry.Series[timeframe] = derived;
            return derived;
        }

        return null;
    }

    public bool ContainsSymbol(string symbol)
        => _symbols.ContainsKey(SymbolRegistry.Normalize(symbol));

    public IReadOnlyList<string> SymbolsInLruOrder()
        => _lru.ToList();

    private void Touch(string key)
    {
        if (_symbols.TryGetValue(key, out var existing))
        {
            TouchExisting(key, existing);
            return;
        }

        // Evict if at capacity
        while (_symbols.Count >= MaxSymbols && _lru.Last != null)
        {
            var victim = _lru.Last.Value;
            _lru.RemoveLast();
            _symbols.Remove(victim);
        }

        var entry = new SymbolEntry();
        var node = _lru.AddFirst(key);
        entry.LruNode = node;
        _symbols[key] = entry;
    }

    private void TouchExisting(string key, SymbolEntry entry)
    {
        if (entry.LruNode != null)
        {
            _lru.Remove(entry.LruNode);
            entry.LruNode = _lru.AddFirst(key);
        }
    }
}
