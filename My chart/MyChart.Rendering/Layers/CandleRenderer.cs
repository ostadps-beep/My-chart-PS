using MyChart.Core.Contracts.Rendering;
using MyChart.Core.Contracts.UI;
using MyChart.Core.Models.Market;
using MyChart.Core.Models.Rendering;
using MyChart.Core.Scale;

namespace MyChart.Rendering.Layers;

/// <summary>
/// T4.04 CandleRenderer — geometry from CandleGeometryCalculator; colors from IThemeService.
/// Calls FillRect only (wicks as 1px fills). No math in the renderer body.
/// </summary>
public sealed class CandleRenderer
{
    private readonly IThemeService _theme;

    public CandleRenderer(IThemeService theme) => _theme = theme;

    public CandleRenderStyle Style { get; set; } = CandleRenderStyle.Candles;

    /// <summary>Highlighted bar index from crosshair snap (null = none).</summary>
    public int? HighlightedIndex { get; set; }

    public void Render(
        IRenderContext ctx,
        IReadOnlyList<Candle> candles,
        int firstIndex,
        CoordinateConverter converter,
        double dpiScale,
        int digits = 5)
    {
        var tokens = _theme.Current;
        for (int i = 0; i < candles.Count; i++)
        {
            int barIndex = firstIndex + i;
            var g = CandleGeometryCalculator.Compute(
                candles[i], barIndex, converter, dpiScale, digits);

            RgbaColor dir = g.IsBull ? tokens.BullColor : tokens.BearColor;
            RgbaColor wick = tokens.WickColor.Argb == 0 ? dir : tokens.WickColor;
            RgbaColor border = tokens.BorderColor.Argb == 0 ? dir : tokens.BorderColor;

            switch (Style)
            {
                case CandleRenderStyle.Ohlc:
                    PaintOhlc(ctx, g, dir);
                    break;
                case CandleRenderStyle.HollowCandles:
                    PaintHollow(ctx, g, dir, wick, border);
                    break;
                default:
                    PaintSolid(ctx, g, dir, wick, border);
                    break;
            }

            if (HighlightedIndex == barIndex)
                PaintHighlight(ctx, g, tokens.SelectionColor);
        }
    }

    /// <summary>Paint a single precomputed geometry (for golden VERIFY tests).</summary>
    public void PaintGeom(IRenderContext ctx, CandleGeom g, bool highlight = false)
    {
        var tokens = _theme.Current;
        RgbaColor dir = g.IsBull ? tokens.BullColor : tokens.BearColor;
        RgbaColor wick = tokens.WickColor.Argb == 0 ? dir : tokens.WickColor;
        RgbaColor border = tokens.BorderColor.Argb == 0 ? dir : tokens.BorderColor;

        switch (Style)
        {
            case CandleRenderStyle.Ohlc:
                PaintOhlc(ctx, g, dir);
                break;
            case CandleRenderStyle.HollowCandles:
                PaintHollow(ctx, g, dir, wick, border);
                break;
            default:
                PaintSolid(ctx, g, dir, wick, border);
                break;
        }

        if (highlight)
            PaintHighlight(ctx, g, tokens.SelectionColor);
    }

    private static void PaintSolid(IRenderContext ctx, CandleGeom g, RgbaColor dir, RgbaColor wick, RgbaColor border)
    {
        // Wick: thin FillRect high→low
        int wickH = Math.Max(1, g.YLow - g.YHigh);
        ctx.FillRect(g.WickLeft, g.YHigh, Math.Max(1, g.WickThickness), wickH, wick);

        if (g.Mode == CandleDrawMode.PixelColumn)
        {
            ctx.FillRect(g.Cx, g.YHigh, 1, wickH, dir);
            return;
        }

        if (g.Mode == CandleDrawMode.WickOnly)
            return;

        // Body fill
        ctx.FillRect(g.BodyLeft, g.BodyTop, g.BodyWidth, Math.Max(1, g.BodyHeight), dir);
    }

    private static void PaintHollow(IRenderContext ctx, CandleGeom g, RgbaColor dir, RgbaColor wick, RgbaColor border)
    {
        int wickH = Math.Max(1, g.YLow - g.YHigh);
        ctx.FillRect(g.WickLeft, g.YHigh, Math.Max(1, g.WickThickness), wickH, wick);

        if (g.Mode != CandleDrawMode.FullCandle)
        {
            if (g.Mode == CandleDrawMode.PixelColumn)
                ctx.FillRect(g.Cx, g.YHigh, 1, wickH, dir);
            return;
        }

        if (g.IsHollow)
        {
            // hollow body: border only via 1px edge fills
            int t = 1;
            int w = g.BodyWidth;
            int h = Math.Max(1, g.BodyHeight);
            // top
            ctx.FillRect(g.BodyLeft, g.BodyTop, w, t, border);
            // bottom
            ctx.FillRect(g.BodyLeft, g.BodyTop + h - t, w, t, border);
            // left
            ctx.FillRect(g.BodyLeft, g.BodyTop, t, h, border);
            // right
            ctx.FillRect(g.BodyLeft + w - t, g.BodyTop, t, h, border);
        }
        else
        {
            // bear or narrow body: solid fill
            ctx.FillRect(g.BodyLeft, g.BodyTop, g.BodyWidth, Math.Max(1, g.BodyHeight), dir);
        }
    }

    private static void PaintOhlc(IRenderContext ctx, CandleGeom g, RgbaColor dir)
    {
        // Vertical high-low
        int wickH = Math.Max(1, g.YLow - g.YHigh);
        ctx.FillRect(g.Cx, g.YHigh, 1, wickH, dir);

        // Open tick left, close tick right (3 px horizontal)
        int tick = 3;
        // Open is at body top if bull else body bottom — use body edges as open/close y
        int yOpen = g.IsBull ? g.BodyBottom : g.BodyTop; // O below C when bull? Wait:
        // IsBull => Close >= Open => yClose <= yOpen in screen Y (down). bodyTop=min(yO,yC)=yClose, bodyBottom=yOpen
        // Actually: bodyTop = Min(yOpen,yClose), bodyBottom = Max(yOpen,yClose)
        // Bull: close higher price => smaller Y => bodyTop = yClose, bodyBottom = yOpen
        yOpen = g.IsBull ? g.BodyBottom : g.BodyTop;
        int yClose = g.IsBull ? g.BodyTop : g.BodyBottom;
        if (g.IsDoji)
        {
            yOpen = g.BodyTop;
            yClose = g.BodyTop;
        }

        ctx.FillRect(g.Cx - tick, yOpen, tick, 1, dir);
        ctx.FillRect(g.Cx + 1, yClose, tick, 1, dir);
    }

    private static void PaintHighlight(IRenderContext ctx, CandleGeom g, RgbaColor selection)
    {
        // 1 px SelectionColor outline around High-Low box, 1 px larger than body on each side
        int left = g.BodyLeft - 1;
        int top = g.YHigh - 1;
        int w = g.BodyWidth + 2;
        int h = Math.Max(1, g.YLow - g.YHigh) + 2;
        // outline via four edges
        ctx.FillRect(left, top, w, 1, selection);
        ctx.FillRect(left, top + h - 1, w, 1, selection);
        ctx.FillRect(left, top, 1, h, selection);
        ctx.FillRect(left + w - 1, top, 1, h, selection);
    }
}
