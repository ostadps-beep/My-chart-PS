using MyChart.Core.Candles;
using MyChart.Core.Models.Market;

namespace MyChart.Data.Aggregation;

/// <summary>
/// T1.11 AggregationEngine (L3) — SetBaseHistory / OnTick / GetSeries.
/// Series mutation only on chart thread (caller responsibility).
/// </summary>
public sealed class AggregationEngine
{
    private readonly SessionCalendar _calendar;
    private readonly Dictionary<string, SymbolState> _states = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Timeframe[] TickDriven =
    {
        Timeframe.M1, Timeframe.M5, Timeframe.M15, Timeframe.M30,
        Timeframe.H1, Timeframe.H4, Timeframe.D1
    };

    public AggregationEngine(SessionCalendar? calendar = null)
    {
        _calendar = calendar ?? new SessionCalendar(ServerTimeRule.Utc, SymbolGroup.Forex);
    }

    public void SetBaseHistory(string symbol, IReadOnlyList<Candle> candles)
    {
        var key = symbol.ToUpperInvariant();
        var state = new SymbolState();
        state.ClosedM1.AddRange(candles.OrderBy(c => c.Timestamp));
        foreach (var tf in TickDriven)
        {
            if (tf == Timeframe.M1) continue;
            var agg = CandleFold.Aggregate(state.ClosedM1, Timeframe.M1, tf, _calendar);
            state.Closed[tf] = agg;
        }
        state.Closed[Timeframe.M1] = state.ClosedM1.ToList();
        _states[key] = state;
    }

    public void OnTick(string symbol, Tick tick)
    {
        var key = symbol.ToUpperInvariant();
        if (!_states.TryGetValue(key, out var state))
        {
            state = new SymbolState();
            _states[key] = state;
        }

        foreach (var tf in TickDriven)
        {
            if (!state.Aggregators.TryGetValue(tf, out var agg))
            {
                agg = new TickAggregator(tf, _calendar);
                // seed with last closed if any
                state.Aggregators[tf] = agg;
            }

            if (agg.Apply(tick, out var closed) && closed.HasValue)
            {
                if (!state.Closed.TryGetValue(tf, out var list))
                {
                    list = new List<Candle>();
                    state.Closed[tf] = list;
                }
                list.Add(closed.Value);
                if (tf == Timeframe.M1)
                    state.ClosedM1.Add(closed.Value);
            }
        }
    }

    public ISeriesView GetSeries(string symbol, Timeframe tf)
    {
        var key = symbol.ToUpperInvariant();
        if (!_states.TryGetValue(key, out var state))
            return SeriesView.Empty;

        state.Closed.TryGetValue(tf, out var closed);
        closed ??= new List<Candle>();

        FormingCandle forming = default;
        bool hasForming = false;
        if (state.Aggregators.TryGetValue(tf, out var agg) && !agg.Forming.IsEmpty)
        {
            forming = agg.Forming;
            hasForming = true;
        }

        return new SeriesView(closed, forming, hasForming);
    }

    private sealed class SymbolState
    {
        public List<Candle> ClosedM1 { get; } = new();
        public Dictionary<Timeframe, List<Candle>> Closed { get; } = new();
        public Dictionary<Timeframe, TickAggregator> Aggregators { get; } = new();
    }

    private sealed class SeriesView : ISeriesView
    {
        private readonly IReadOnlyList<Candle> _closed;
        private readonly FormingCandle _forming;
        private readonly bool _hasForming;

        public static SeriesView Empty { get; } = new(Array.Empty<Candle>(), default, false);

        public SeriesView(IReadOnlyList<Candle> closed, FormingCandle forming, bool hasForming)
        {
            _closed = closed;
            _forming = forming;
            _hasForming = hasForming;
        }

        public int Count => _closed.Count + (_hasForming ? 1 : 0);
        public bool HasForming => _hasForming;

        public Candle this[int index]
        {
            get
            {
                if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
                if (_hasForming && index == Count - 1)
                    return _forming.ToCandle();
                return _closed[index];
            }
        }

        public DateTimeOffset OpenTime(int index) => this[index].Timestamp;
    }
}
