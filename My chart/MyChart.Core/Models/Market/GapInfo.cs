namespace MyChart.Core.Models.Market;

/// <summary>
/// T1.03 — gap between two neighbouring accepted candles.
/// MissingBars = number of timeframe slots strictly between the two neighbours.
/// </summary>
public sealed record GapInfo(
    DateTimeOffset From,
    DateTimeOffset To,
    int MissingBars,
    GapKind Kind);
