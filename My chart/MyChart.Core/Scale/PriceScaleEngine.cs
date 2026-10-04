using MyChart.Core.Models.Market;
using MyChart.Core.Models.Viewport;

namespace MyChart.Core.Scale;

/// <summary>T2.07 PriceScaleEngine — Auto/Manual fit and transforms.</summary>
public static class PriceScaleEngine
{
    public static void SetAuto(PriceScaleState state)
    {
        state.Fit = ScaleFit.Auto;
        state.IsManualLocked = false;
    }

    public static void SetManual(PriceScaleState state, double minT, double maxT)
    {
        state.Fit = ScaleFit.Manual;
        state.IsManualLocked = true;
        state.ManualMin = minT;
        state.ManualMax = maxT;
        state.MinPrice = state.InverseTransform(minT);
        state.MaxPrice = state.InverseTransform(maxT);
    }

    /// <summary>
    /// Auto range over bars [from..to].
    /// pad 10% top and bottom of span'; min span 20*PointSize in price space.
    /// </summary>
    public static void ComputeAuto(
        PriceScaleState state,
        IReadOnlyList<Candle> bars,
        int from,
        int to,
        double pointSize,
        double? percentageBaseClose = null)
    {
        if (state.Fit != ScaleFit.Auto) return;
        if (bars == null || bars.Count == 0 || from > to || from < 0)
            return; // keep previous range

        to = Math.Min(to, bars.Count - 1);
        from = Math.Max(0, from);
        if (from > to) return;

        double hi = double.MinValue, lo = double.MaxValue;
        bool allLowPositive = true;
        for (int i = from; i <= to; i++)
        {
            var c = bars[i];
            if (c.High > hi) hi = c.High;
            if (c.Low < lo) lo = c.Low;
            if (c.Low <= 0) allLowPositive = false;
        }

        if (state.Transform == ScaleTransformKind.Log && !allLowPositive)
        {
            state.Transform = ScaleTransformKind.Linear;
            state.LogUnavailable = true;
        }
        else
        {
            state.LogUnavailable = false;
        }

        if (state.Transform == ScaleTransformKind.Percentage)
            state.PercentageBase = percentageBaseClose ?? bars[from].Close;

        // min span in price space
        double spanPrice = hi - lo;
        double minSpan = 20 * pointSize;
        if (spanPrice < minSpan)
        {
            double mid = (hi + lo) * 0.5;
            hi = mid + minSpan * 0.5;
            lo = mid - minSpan * 0.5;
        }

        double hiT = state.TransformPrice(hi);
        double loT = state.TransformPrice(lo);
        double spanT = hiT - loT;
        if (spanT < 0) (hiT, loT, spanT) = (loT, hiT, -spanT);

        // pad 10% each side in transform space
        double pad = spanT * 0.10;
        hiT += pad;
        loT -= pad;

        state.MinPrice = state.InverseTransform(loT);
        state.MaxPrice = state.InverseTransform(hiT);
    }

    /// <summary>Price-axis drag zoom: factor = clamp(1 + (-dy)*0.005, 0.5, 2.0) anchored at start price.</summary>
    public static void ManualDragZoom(PriceScaleState state, double anchorPrice, double dyDip)
    {
        double factor = Math.Clamp(1.0 + (-dyDip) * 0.005, 0.5, 2.0);
        double aT = state.TransformPrice(anchorPrice);
        double minT = state.TransformPrice(state.MinPrice);
        double maxT = state.TransformPrice(state.MaxPrice);

        minT = aT + (minT - aT) * factor;
        maxT = aT + (maxT - aT) * factor;

        EnsureMinSpan(state, ref minT, ref maxT, pointSize: 0.00001);
        SetManual(state, minT, maxT);
    }

    private static void EnsureMinSpan(PriceScaleState state, ref double minT, ref double maxT, double pointSize)
    {
        double minSpan = state.Transform switch
        {
            ScaleTransformKind.Log => 1e-5,
            ScaleTransformKind.Percentage => 0.01,
            _ => 5 * pointSize
        };
        if (maxT - minT < minSpan)
        {
            double mid = (maxT + minT) * 0.5;
            minT = mid - minSpan * 0.5;
            maxT = mid + minSpan * 0.5;
        }
    }
}
