using MyChart.Core.Models.Market;

namespace MyChart.Core.Candles;

/// <summary>
/// T1.04 SymbolMath — PointSize and PipSize pure helpers.
/// </summary>
public static class SymbolMath
{
    public static double PointSize(int digits)
        => Math.Pow(10, -digits);

    /// <summary>
    /// Forex only: Digits 3 or 5 → PointSize * 10; Digits 2 or 4 → PointSize.
    /// Other groups: undefined (null).
    /// </summary>
    public static double? PipSize(SymbolGroup group, int digits)
    {
        if (group != SymbolGroup.Forex)
            return null;

        var point = PointSize(digits);
        return digits is 3 or 5 ? point * 10 : point;
    }
}
