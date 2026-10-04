using MyChart.Core.Models.Market;

namespace MyChart.Core.Models.Chart;

/// <summary>T3.02 HUD presentation, chosen by the chart plot width (ADAPTIVE).</summary>
public enum HudPresentation
{
    Full,
    Compact,
    Minimal
}

/// <summary>T3.02 MarketStatus shown in the HUD.</summary>
public enum HudMarketStatus
{
    Replay,
    Offline,
    Live,
    Stale
}

/// <summary>T3.02 CacheStatus shown in the HUD (ADVANCED_OPTIONAL).</summary>
public enum HudCacheStatus
{
    Memory,
    Disk,
    Empty
}

/// <summary>
/// T3.02 HUD fields. Information = the INFORMATION list; Advanced = the ADVANCED_OPTIONAL list
/// (shown only in the Full presentation and only when the user enabled them).
/// </summary>
[Flags]
public enum HudField
{
    None = 0,

    Symbol = 1 << 0,
    Timeframe = 1 << 1,
    OhlcUnderMouse = 1 << 2,
    Spread = 1 << 3,
    MarketStatus = 1 << 4,
    VisibleBars = 1 << 5,
    Start = 1 << 6,
    End = 1 << 7,

    ZoomLevel = 1 << 8,
    Fps = 1 << 9,
    LoadedCandles = 1 << 10,
    ProviderName = 1 << 11,
    CacheStatus = 1 << 12,

    Information = Symbol | Timeframe | OhlcUnderMouse | Spread | MarketStatus | VisibleBars | Start | End,
    Advanced = ZoomLevel | Fps | LoadedCandles | ProviderName | CacheStatus
}

/// <summary>
/// T3.02 result of one HUD calculation. Visible lists the fields the presentation shows.
/// OhlcIndex is the series index of OhlcUnderMouse (-1 when the series is empty).
/// SpreadPoints is null until a tick was accepted. Start and End are in the display time zone.
/// </summary>
public sealed record HudState
{
    public string Symbol { get; init; } = string.Empty;
    public Timeframe Timeframe { get; init; }
    public HudPresentation Presentation { get; init; }
    public HudField Visible { get; init; }
    public Candle? OhlcUnderMouse { get; init; }
    public int OhlcIndex { get; init; } = -1;
    public int? SpreadPoints { get; init; }
    public HudMarketStatus MarketStatus { get; init; }
    public int VisibleBars { get; init; }
    public DateTimeOffset? Start { get; init; }
    public DateTimeOffset? End { get; init; }
    public int ZoomLevelPercent { get; init; }
    public int LoadedCandles { get; init; }
    public string ProviderName { get; init; } = string.Empty;
    public HudCacheStatus CacheStatus { get; init; }
    public double Fps { get; init; }
}
