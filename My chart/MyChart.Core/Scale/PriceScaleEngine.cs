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
    /// Zoom price scale around a fixed price (stays at same screen Y).
    /// factor &gt; 1 = zoom out (wider range); factor &lt; 1 = zoom in.
    /// Same geometry as Y(price) = plotTop + (maxT-pT)/(maxT-minT)*plotHeight.
    /// </summary>
    public static void ZoomAroundPrice(PriceScaleState state, double anchorPrice, double factor)
    {
        factor = Math.Clamp(factor, 0.15, 8.0);
        double aT = state.TransformPrice(anchorPrice);
        double minT = state.TransformPrice(state.MinPrice);
        double maxT = state.TransformPrice(state.MaxPrice);
        if (maxT <= minT)
            maxT = minT + 1e-12;

        double newMinT = aT + (minT - aT) * factor;
        double newMaxT = aT + (maxT - aT) * factor;
        EnsureMinSpan(state, ref newMinT, ref newMaxT, pointSize: 1e-8);
        SetManual(state, newMinT, newMaxT);
    }

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
            return;

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

        if (state.TransformKind == ScaleTransformKind.Log && !allLowPositive)
        {
            state.TransformKind = ScaleTransformKind.Linear;
            state.LogUnavailable = true;
        }
        else
        {
            state.LogUnavailable = false;
        }

        if (state.TransformKind == ScaleTransformKind.Percentage)
            state.PercentageBase = percentageBaseClose ?? bars[from].Close;

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

        double pad = spanT * 0.10;
        hiT += pad;
        loT -= pad;

        state.MinPrice = state.InverseTransform(loT);
        state.MaxPrice = state.InverseTransform(hiT);
    }

    public static void ManualDragZoom(PriceScaleState state, double anchorPrice, double dyDip)
    {
        // Legacy path: one-shot factor from dy (prefer ZoomAroundPrice for continuous drag)
        double factor = Math.Clamp(1.0 + (-dyDip) * 0.005, 0.5, 2.0);
        ZoomAroundPrice(state, anchorPrice, factor);
    }

    private static void EnsureMinSpan(PriceScaleState state, ref double minT, ref double maxT, double pointSize)
    {
        double minSpan = state.TransformKind switch
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
