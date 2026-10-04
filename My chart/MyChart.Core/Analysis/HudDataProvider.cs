using System.Globalization;
using MyChart.Core.Candles;
using MyChart.Core.Models.Chart;
using MyChart.Core.Models.Market;
using MyChart.Core.Models.Viewport;

namespace MyChart.Core.Analysis;

/// <summary>
/// T3.02 inputs of one HUD calculation. Everything comes from the chart session; nothing is read from the UI.
/// Bars is the displayed series with the forming candle as its last element.
/// ServerNow = IClock.UtcNow + ClockSkew (the same clock as the countdown of T2.10).
/// ProviderConnected is false when the provider is disconnected or chart.offline is on.
/// EnabledAdvanced lists the ADVANCED_OPTIONAL fields the user turned on.
/// </summary>
public sealed record HudInput(
    SymbolInfo Symbol,
    Timeframe Timeframe,
    IReadOnlyList<Candle> Bars,
    RenderWindowRange Range,
    double BarSpacing,
    double PlotWidth,
    CrosshairState? Crosshair,
    Tick? LastTick,
    DateTimeOffset ServerNow,
    bool ReplayActive,
    bool ProviderConnected,
    string ProviderName,
    HudCacheStatus CacheStatus,
    double Fps,
    HudField EnabledAdvanced,
    TimeZoneInfo? DisplayZone = null);

/// <summary>
/// T3.02 HudData — pure calculation of the HUD fields, the adaptive presentation and the status rules.
/// </summary>
public static class HudDataProvider
{
    /// <summary>Plot width (DIP) from which the Full presentation is used.</summary>
    public const double FullMinWidthDip = 900;

    /// <summary>Plot width (DIP) from which the Compact presentation is used (500..899).</summary>
    public const double CompactMinWidthDip = 500;

    /// <summary>The last tick counts as Live when it is at most this old.</summary>
    public static readonly TimeSpan LiveMaxAge = TimeSpan.FromSeconds(30);

    /// <summary>BarSpacing that equals 100 percent zoom (DIP).</summary>
    public const double ZoomBaseBarSpacing = 6;

    public static HudState Compute(HudInput input)
    {
        var bars = input.Bars;
        var presentation = PresentationFor(input.PlotWidth);
        int ohlcIndex = OhlcIndex(input.Crosshair, bars.Count);

        Candle? ohlc = ohlcIndex >= 0 ? bars[ohlcIndex] : null;

        DateTimeOffset? start = null;
        DateTimeOffset? end = null;
        var r = input.Range;
        if (r.VisibleBarCount > 0 && r.From >= 0 && r.To >= r.From && r.To <= bars.Count - 1)
        {
            start = ToDisplay(bars[r.From].Timestamp, input.DisplayZone);
            end = ToDisplay(bars[r.To].Timestamp, input.DisplayZone);
        }

        return new HudState
        {
            Symbol = input.Symbol.Name,
            Timeframe = input.Timeframe,
            Presentation = presentation,
            Visible = VisibleFields(presentation, input.EnabledAdvanced),
            OhlcUnderMouse = ohlc,
            OhlcIndex = ohlcIndex,
            SpreadPoints = SpreadPoints(input.LastTick, input.Symbol),
            MarketStatus = MarketStatusOf(input.ReplayActive, input.ProviderConnected, input.LastTick, input.ServerNow),
            VisibleBars = r.VisibleBarCount,
            Start = start,
            End = end,
            ZoomLevelPercent = ZoomLevelPercent(input.BarSpacing),
            LoadedCandles = bars.Count,
            ProviderName = input.ProviderName,
            CacheStatus = input.CacheStatus,
            Fps = input.Fps
        };
    }

    /// <summary>ADAPTIVE by chart plot width: &gt;= 900 Full ; 500..899 Compact ; &lt; 500 Minimal.</summary>
    public static HudPresentation PresentationFor(double plotWidth)
    {
        if (plotWidth >= FullMinWidthDip) return HudPresentation.Full;
        if (plotWidth >= CompactMinWidthDip) return HudPresentation.Compact;
        return HudPresentation.Minimal;
    }

    /// <summary>
    /// Full = INFORMATION list + the ADVANCED_OPTIONAL fields the user enabled ;
    /// Compact = Symbol, Timeframe, OHLCUnderMouse, Spread ; Minimal = Symbol, Timeframe, Spread.
    /// </summary>
    public static HudField VisibleFields(HudPresentation presentation, HudField enabledAdvanced)
    {
        switch (presentation)
        {
            case HudPresentation.Full:
                return HudField.Information | (enabledAdvanced & HudField.Advanced);
            case HudPresentation.Compact:
                return HudField.Symbol | HudField.Timeframe | HudField.OhlcUnderMouse | HudField.Spread;
            default:
                return HudField.Symbol | HudField.Timeframe | HudField.Spread;
        }
    }

    /// <summary>
    /// OHLCUnderMouse = the data-inspector candle (index of the snapped bar while the pointer is inside the plot
    /// and 0 &lt;= iSnap &lt;= N-1); outside the plot or in a future slot = the latest candle (forming included).
    /// Returns -1 when the series is empty.
    /// </summary>
    public static int OhlcIndex(CrosshairState? crosshair, int barCount)
    {
        if (barCount <= 0) return -1;

        if (crosshair != null
            && crosshair.IsInsidePlot
            && crosshair.SnapIndex >= 0
            && crosshair.SnapIndex <= barCount - 1)
        {
            return crosshair.SnapIndex;
        }

        return barCount - 1;
    }

    /// <summary>Spread = last accepted tick (Ask - Bid) / PointSize, integer points; null when no tick yet.</summary>
    public static int? SpreadPoints(Tick? lastTick, SymbolInfo symbol)
    {
        if (!lastTick.HasValue) return null;

        double point = SymbolMath.PointSize(symbol.Digits);
        double spread = (lastTick.Value.Ask - lastTick.Value.Bid) / point;
        return (int)Math.Round(spread, MidpointRounding.AwayFromZero);
    }

    /// <summary>Spread text: the integer points, or "-" when no tick yet.</summary>
    public static string FormatSpread(int? spreadPoints)
        => spreadPoints.HasValue ? spreadPoints.Value.ToString(CultureInfo.InvariantCulture) : "-";

    /// <summary>
    /// MarketStatus: Replay when the replay provider is active ; Offline when the provider is disconnected ;
    /// Live when the last tick is at most 30 s old ; Stale otherwise (also when no tick arrived yet).
    /// A tick time ahead of ServerNow counts as Live.
    /// </summary>
    public static HudMarketStatus MarketStatusOf(
        bool replayActive,
        bool providerConnected,
        Tick? lastTick,
        DateTimeOffset serverNow)
    {
        if (replayActive) return HudMarketStatus.Replay;
        if (!providerConnected) return HudMarketStatus.Offline;

        if (lastTick.HasValue && serverNow - lastTick.Value.Timestamp <= LiveMaxAge)
            return HudMarketStatus.Live;

        return HudMarketStatus.Stale;
    }

    /// <summary>ZoomLevel = round(BarSpacing / 6 * 100) percent (6 DIP = 100 percent).</summary>
    public static int ZoomLevelPercent(double barSpacing)
        => (int)Math.Round(barSpacing / ZoomBaseBarSpacing * 100.0, MidpointRounding.AwayFromZero);

    private static DateTimeOffset ToDisplay(DateTimeOffset utc, TimeZoneInfo? displayZone)
        => TimeZoneInfo.ConvertTime(utc, displayZone ?? TimeZoneInfo.Utc);
}
