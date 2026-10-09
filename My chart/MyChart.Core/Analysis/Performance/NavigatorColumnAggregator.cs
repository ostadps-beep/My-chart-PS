namespace MyChart.Core.Analysis.Performance;

/// <summary>
/// T7.03 NavigatorSimplifiedRendering — min/max high/low per pixel column.
/// </summary>
public static class NavigatorColumnAggregator
{
    public readonly record struct Column(double Min, double Max);

    /// <summary>
    /// Packs [0..n) bar highs/lows into <paramref name="pixelWidth"/> columns.
    /// Empty bars → empty result.
    /// </summary>
    public static Column[] Aggregate(IReadOnlyList<double> highs, IReadOnlyList<double> lows, int pixelWidth)
    {
        if (highs is null || lows is null) throw new ArgumentNullException();
        if (highs.Count != lows.Count) throw new ArgumentException("highs/lows length mismatch");
        if (pixelWidth <= 0 || highs.Count == 0)
            return Array.Empty<Column>();

        var n = highs.Count;
        var cols = new Column[pixelWidth];
        for (var c = 0; c < pixelWidth; c++)
            cols[c] = new Column(double.PositiveInfinity, double.NegativeInfinity);

        for (var i = 0; i < n; i++)
        {
            var c = (int)((long)i * pixelWidth / n);
            if (c >= pixelWidth) c = pixelWidth - 1;
            var hi = highs[i];
            var lo = lows[i];
            var cur = cols[c];
            cols[c] = new Column(Math.Min(cur.Min, lo), Math.Max(cur.Max, hi));
        }

        // columns with no bars stay empty (Min > Max)
        return cols;
    }

    public static bool IsEmpty(Column c) => c.Min > c.Max || double.IsInfinity(c.Min);
}
