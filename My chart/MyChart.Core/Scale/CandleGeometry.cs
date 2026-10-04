using MyChart.Core.Models.Market;
using MyChart.Core.Models.Viewport;

namespace MyChart.Core.Scale;

public enum CandleDrawMode
{
    FullCandle,
    WickOnly,
    PixelColumn
}

/// <summary>T2.09 integer device-pixel geometry for one candle. No Skia.</summary>
public readonly record struct CandleGeom(
    int Cx,
    int WickLeft,
    int WickThickness,
    int BodyLeft,
    int BodyWidth,
    int BodyTop,
    int BodyBottom,
    int BodyHeight,
    int YHigh,
    int YLow,
    bool IsBull,
    bool IsDoji,
    bool IsHollow,
    CandleDrawMode Mode);

/// <summary>T2.09 CandleGeometryCalculator — pure; all outputs integers.</summary>
public static class CandleGeometryCalculator
{
    public const double DefaultGapDip = 2;
    public const int DefaultWickThickness = 1;

    public static CandleDrawMode ModeFor(double barSpacingDip, double dpiScale, bool showBody = true)
    {
        if (!showBody) return CandleDrawMode.WickOnly;
        double spacingDev = barSpacingDip * dpiScale;
        if (spacingDev >= 3) return CandleDrawMode.FullCandle;
        if (spacingDev >= 2) return CandleDrawMode.WickOnly;
        return CandleDrawMode.PixelColumn;
    }

    public static CandleGeom Compute(
        Candle candle,
        int barIndex,
        CoordinateConverter cc,
        double dpiScale,
        int digits = 5,
        double gapDip = DefaultGapDip,
        int wickThickness = DefaultWickThickness,
        bool showBody = true,
        bool showWicks = true)
    {
        double d = dpiScale;
        double spacingDev = cc.ViewState.BarSpacing * d;
        var mode = ModeFor(cc.ViewState.BarSpacing, d, showBody);

        int cx = (int)Math.Floor(cc.X(barIndex) * d);
        int wickLeft = cx - (wickThickness - 1) / 2;

        int yOpen = (int)Math.Round(cc.Y(candle.Open) * d);
        int yClose = (int)Math.Round(cc.Y(candle.Close) * d);
        int yHigh = (int)Math.Round(cc.Y(candle.High) * d);
        int yLow = (int)Math.Round(cc.Y(candle.Low) * d);
        // Y grows downward; High is smaller Y
        if (yLow < yHigh) (yLow, yHigh) = (yHigh, yLow);

        bool isBull = candle.Close >= candle.Open;
        double roundedOpen = Math.Round(candle.Open, digits, MidpointRounding.AwayFromZero);
        double roundedClose = Math.Round(candle.Close, digits, MidpointRounding.AwayFromZero);
        bool isDoji = roundedOpen == roundedClose;

        int bodyTop = Math.Min(yOpen, yClose);
        int bodyBottom = Math.Max(yOpen, yClose);
        int bodyHeight = Math.Max(1, bodyBottom - bodyTop);
        if (isDoji) bodyHeight = 1;

        int bodyWidth = 1;
        int bodyLeft = cx;
        bool isHollow = false;

        if (mode == CandleDrawMode.FullCandle)
        {
            int gapDev = (int)Math.Floor(gapDip * d);
            bodyWidth = (int)Math.Floor(spacingDev - gapDev);
            if (bodyWidth % 2 == 0) bodyWidth -= 1;
            bodyWidth = Math.Max(1, bodyWidth);
            int maxW = Math.Max(1, (int)Math.Floor(spacingDev) - 1);
            if (bodyWidth > maxW) bodyWidth = maxW;
            bodyWidth = Math.Max(1, bodyWidth);
            bodyLeft = cx - (bodyWidth - 1) / 2;
            // HOLLOW: bull hollow when bodyWidth >= 3
            isHollow = isBull && bodyWidth >= 3;
        }

        if (!showWicks)
        {
            yHigh = bodyTop;
            yLow = bodyBottom;
        }

        return new CandleGeom(
            Cx: cx,
            WickLeft: wickLeft,
            WickThickness: showWicks ? wickThickness : 0,
            BodyLeft: bodyLeft,
            BodyWidth: bodyWidth,
            BodyTop: bodyTop,
            BodyBottom: bodyTop + bodyHeight,
            BodyHeight: bodyHeight,
            YHigh: yHigh,
            YLow: yLow,
            IsBull: isBull,
            IsDoji: isDoji,
            IsHollow: isHollow,
            Mode: mode);
    }
}
