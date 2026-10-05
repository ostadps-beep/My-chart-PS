namespace MyChart.Core.Models.Drawing;

/// <summary>
/// T3.04 ANCHOR = (TimeUtc, Price). Stored per symbol and visible on all timeframes, so an anchor never holds a bar index.
/// The screen position is (X(IndexOfTime(TimeUtc)), Y(Price)).
/// </summary>
public readonly record struct DrawingAnchor(DateTimeOffset TimeUtc, double Price);
