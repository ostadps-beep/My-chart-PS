using System.Globalization;
using MyChart.Core.Models.Viewport;

namespace MyChart.Core.Scale;

/// <summary>T2.08 NiceTicksAndAxisLabelPlanning — price ticks in transform space.</summary>
public static class NiceTicks
{
    public const double TargetSpacingDip = 50;
    public const double MinSpacingDip = 40;

    private static readonly double[] Mantissas = { 1, 2, 2.5, 5, 10 };

    public static double ComputeStep(double spanTransform, double plotHeight, double pointSize)
    {
        if (plotHeight <= 0 || spanTransform <= 0)
            return Math.Max(pointSize, 1e-12);

        double rawStep = spanTransform / (plotHeight / TargetSpacingDip);
        if (rawStep <= 0) rawStep = pointSize;

        double mag = Math.Pow(10, Math.Floor(Math.Log10(rawStep)));
        double step = Mantissas[0] * mag;
        foreach (var m in Mantissas)
        {
            double candidate = m * mag;
            if (candidate >= rawStep)
            {
                step = candidate;
                break;
            }
        }

        if (step < pointSize) step = pointSize;
        return step;
    }

    public static List<double> PriceTicks(double minT, double maxT, double step)
    {
        var ticks = new List<double>();
        if (step <= 0 || maxT < minT) return ticks;

        double first = Math.Ceiling(minT / step) * step;
        // guard floating noise
        for (double t = first; t <= maxT + step * 1e-9; t += step)
        {
            double v = Math.Round(t / step) * step;
            if (v >= minT - step * 1e-9 && v <= maxT + step * 1e-9)
                ticks.Add(v);
            if (ticks.Count > 10_000) break;
        }
        return ticks;
    }

    public static double MinorStep(double step)
    {
        // identify mantissa
        double mag = Math.Pow(10, Math.Floor(Math.Log10(step)));
        double mantissa = step / mag;
        double div = mantissa switch
        {
            2 => 4,
            _ => 5
        };
        return step / div;
    }

    public static string FormatPriceLabel(double price, double step, int digits, ScaleTransformKind kind)
    {
        if (kind == ScaleTransformKind.Percentage)
            return (price >= 0 ? "+" : "") + price.ToString("0.00", CultureInfo.InvariantCulture) + "%";

        int decimals = DecimalsForStep(step, digits);
        return price.ToString("F" + decimals, CultureInfo.InvariantCulture);
    }

    public static int DecimalsForStep(double step, int digits)
    {
        if (step >= 1) return 0;
        int needed = (int)Math.Ceiling(-Math.Log10(step));
        return Math.Min(digits, Math.Max(0, needed));
    }

    /// <summary>Label count must never exceed plotHeight/40.</summary>
    public static int MaxPriceLabels(double plotHeight)
        => Math.Max(1, (int)(plotHeight / MinSpacingDip));

    public static int MaxTimeLabels(double plotWidth)
        => Math.Max(1, (int)(plotWidth / 80));
}
