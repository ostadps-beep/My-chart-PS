namespace MyChart.Core.Performance;

/// <summary>
/// T7.03 NavigatorSimplifiedRendering — min/max price per pixel column for overview.
/// </summary>
public static class NavigatorColumnSimplifier
{
    public readonly record struct Column(double Min, double Max);

    public static Column[] Simplify(
        IReadOnlyList<double> highs,
        IReadOnlyList<double> lows,
        int pixelWidth)
    {
        ArgumentNullException.ThrowIfNull(highs);
        ArgumentNullException.ThrowIfNull(lows);
        if (highs.Count != lows.Count)
            throw new ArgumentException("highs and lows length mismatch.");
        if (pixelWidth <= 0)
            return Array.Empty<Column>();
        if (highs.Count == 0)
            return Array.Empty<Column>();

        var cols = new Column[pixelWidth];
        double barsPerPx = highs.Count / (double)pixelWidth;

        for (int x = 0; x < pixelWidth; x++)
        {
            int from = (int)Math.Floor(x * barsPerPx);
            int to = (int)Math.Floor((x + 1) * barsPerPx) - 1;
            if (to < from) to = from;
            if (from >= highs.Count) from = highs.Count - 1;
            if (to >= highs.Count) to = highs.Count - 1;

            double min = lows[from];
            double max = highs[from];
            for (int i = from; i <= to; i++)
            {
                if (lows[i] < min) min = lows[i];
                if (highs[i] > max) max = highs[i];
            }

            cols[x] = new Column(min, max);
        }

        return cols;
    }
}
