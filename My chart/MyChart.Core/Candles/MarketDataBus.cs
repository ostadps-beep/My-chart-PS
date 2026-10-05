using System.Collections.Concurrent;
using MyChart.Core.Contracts.Data;
using MyChart.Core.Models.Market;

namespace MyChart.Core.Candles;

/// <summary>
/// T1.07 MarketDataBus — chart-thread publish; exception isolation;
/// CandleUpdated coalesced at most once per (symbol,timeframe) per 16 ms frame;
/// CandleClosed never coalesced or dropped.
/// </summary>
public sealed class MarketDataBus : IMarketDataBus
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _handlers = new();
    private readonly object _gate = new();

    // Coalescing state for CandleUpdated
    private readonly Dictionary<(string Symbol, Timeframe Tf), CandleUpdated> _pendingUpdates = new();
    private DateTimeOffset _frameStart = DateTimeOffset.MinValue;
    private static readonly TimeSpan FrameDuration = TimeSpan.FromMilliseconds(16);

    public IDisposable Subscribe<T>(Action<T> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        var list = _handlers.GetOrAdd(typeof(T), _ => new List<Delegate>());
        lock (_gate)
        {
            list.Add(handler);
        }
        return new Subscription(() =>
        {
            lock (_gate)
            {
                list.Remove(handler);
            }
        });
    }

    public void Publish<T>(T evt)
    {
        if (evt is CandleUpdated cu)
        {
            CoalesceCandleUpdated(cu);
            return;
        }

        // Flush any pending coalesced updates before non-update events if frame elapsed
        FlushCoalescedIfNeeded(DateTimeOffset.UtcNow);
        Dispatch(evt);
    }

    /// <summary>
    /// Force flush coalesced CandleUpdated events (call once per UI frame).
    /// </summary>
    public void FlushFrame()
    {
        List<CandleUpdated> toSend;
        lock (_gate)
        {
            toSend = _pendingUpdates.Values.ToList();
            _pendingUpdates.Clear();
            _frameStart = DateTimeOffset.UtcNow;
        }
        foreach (var e in toSend)
            Dispatch(e);
    }

    private void CoalesceCandleUpdated(CandleUpdated cu)
    {
        lock (_gate)
        {
            if (_frameStart == DateTimeOffset.MinValue)
                _frameStart = DateTimeOffset.UtcNow;

            // Always coalesce into the pending slot for this (symbol, timeframe).
            // Do NOT auto-flush here — wall-clock gaps between publishes must not
            // emit intermediate updates; FlushFrame / other events own the frame boundary.
            _pendingUpdates[(cu.Symbol, cu.Timeframe)] = cu;
        }
    }

    private void FlushCoalescedIfNeeded(DateTimeOffset now)
    {
        List<CandleUpdated>? flush = null;
        lock (_gate)
        {
            if (_pendingUpdates.Count > 0 && now - _frameStart >= FrameDuration)
            {
                flush = _pendingUpdates.Values.ToList();
                _pendingUpdates.Clear();
                _frameStart = now;
            }
        }
        if (flush != null)
        {
            foreach (var e in flush)
                Dispatch(e);
        }
    }

    private void Dispatch<T>(T evt)
    {
        if (!_handlers.TryGetValue(typeof(T), out var list))
            return;

        Delegate[] snapshot;
        lock (_gate)
        {
            snapshot = list.ToArray();
        }

        foreach (var d in snapshot)
        {
            try
            {
                ((Action<T>)d)(evt);
            }
            catch
            {
                // ISOLATION: log would go through ILogService later; swallow to protect other subscribers
            }
        }
    }

    private sealed class Subscription(Action unsubscribe) : IDisposable
    {
        private Action? _unsubscribe = unsubscribe;
        public void Dispose() => Interlocked.Exchange(ref _unsubscribe, null)?.Invoke();
    }
}
