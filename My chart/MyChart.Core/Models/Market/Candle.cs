namespace MyChart.Core.Models.Market;

/// <summary>
/// Immutable pure data candle. No color, brush, theme, style, pixels, index, timeframe or symbol.
/// </summary>
public readonly record struct Candle(
    DateTimeOffset Timestamp,
    double Open,
    double High,
    double Low,
    double Close,
    double Volume)
{
    public bool IsBullish => Close >= Open;
    public bool IsBearish => Close < Open;
}
