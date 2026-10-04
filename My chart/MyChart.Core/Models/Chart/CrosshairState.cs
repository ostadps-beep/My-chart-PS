namespace MyChart.Core.Models.Chart;

/// <summary>
/// T3.01 independent on/off flags. The cursor itself is always on.
/// </summary>
public readonly record struct CrosshairFlags(bool Analysis, bool DataInspector, bool Magnet);

/// <summary>T3.01 Analysis anchor A: aIndex = SnapIndex at the first LeftClick, aPrice = cursor price at that moment.</summary>
public readonly record struct AnalysisAnchor(int Index, double Price);

/// <summary>
/// T3.01 Analysis values between anchor A and the cursor B.
/// PipDifference is null outside the Forex group (hidden).
/// PipDifference is rounded to 1 decimal and PercentageChange to 2 decimals.
/// PercentageChange is NaN when the anchor price is 0.
/// </summary>
public readonly record struct AnalysisValues(
    double PriceDifference,
    long PointDifference,
    double? PipDifference,
    TimeSpan TimeDifference,
    int CandleCount,
    double PercentageChange);

/// <summary>T3.01 DataInspector values of the candle at SnapIndex. DisplayTime is in the display time zone.</summary>
public readonly record struct DataInspectorValues(
    DateTimeOffset DisplayTime,
    double Open,
    double High,
    double Low,
    double Close,
    double Volume);

/// <summary>
/// T3.01 result of one crosshair calculation. All coordinates are DIP.
/// X is the centre of the snapped bar. Y is the mouse Y, or the Y of the magnet price when IsMagnetSnapped.
/// </summary>
public sealed record CrosshairState
{
    public bool IsInsidePlot { get; init; }
    public int SnapIndex { get; init; }
    public double X { get; init; }
    public double Y { get; init; }
    public double Price { get; init; }
    public bool IsMagnetSnapped { get; init; }
    public DateTimeOffset TimeUtc { get; init; }
    public string TimeLabel { get; init; } = string.Empty;
    public string PriceLabel { get; init; } = string.Empty;
    public DataInspectorValues? DataInspector { get; init; }
    public AnalysisValues? Analysis { get; init; }
}
