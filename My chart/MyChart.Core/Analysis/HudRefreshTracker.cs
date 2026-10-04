using MyChart.Core.Models.Chart;

namespace MyChart.Core.Analysis;

/// <summary>
/// T3.02 REFRESH — the HUD is redrawn only on CandleUpdated (coalesced), on a change of the hovered bar
/// (not on every pixel), on a range change and on a MarketStatus change. The FPS field refreshes at 2 Hz.
/// The first ShouldRefresh call always returns true (initial draw).
/// </summary>
public sealed class HudRefreshTracker
{
    /// <summary>FPS field refresh interval (2 Hz).</summary>
    public static readonly TimeSpan FpsInterval = TimeSpan.FromMilliseconds(500);

    private bool _first = true;
    private bool _candleDirty;
    private int _ohlcIndex;
    private int _from;
    private int _to;
    private HudMarketStatus _status;
    private DateTimeOffset? _lastFpsRefresh;

    /// <summary>CandleUpdated arrived. Any number of calls before the next ShouldRefresh cause one refresh.</summary>
    public void NotifyCandleUpdated() => _candleDirty = true;

    /// <summary>
    /// ohlcIndex = HudDataProvider.OhlcIndex(...) (the displayed candle, so pixel moves inside one bar do not count).
    /// </summary>
    public bool ShouldRefresh(int ohlcIndex, int from, int to, HudMarketStatus status)
    {
        bool refresh = _first
                       || _candleDirty
                       || ohlcIndex != _ohlcIndex
                       || from != _from
                       || to != _to
                       || status != _status;

        _first = false;
        _candleDirty = false;
        _ohlcIndex = ohlcIndex;
        _from = from;
        _to = to;
        _status = status;
        return refresh;
    }

    /// <summary>True at most every 500 ms (2 Hz); the first call returns true.</summary>
    public bool ShouldRefreshFps(DateTimeOffset now)
    {
        if (_lastFpsRefresh is null || now - _lastFpsRefresh.Value >= FpsInterval)
        {
            _lastFpsRefresh = now;
            return true;
        }

        return false;
    }
}
