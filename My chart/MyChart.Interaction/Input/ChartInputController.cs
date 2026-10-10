using MyChart.Core.Models.Viewport;
using MyChart.Core.Scale;

namespace MyChart.Interaction.Input;

/// <summary>
/// T4.07 + C2 owner UX:
/// Plot drag = 4 directions (time horizontal + price vertical shift).
/// Price-axis drag = smooth zoom around anchor (from drag start, not stepped multiply).
/// Wheel on plot = time zoom; wheel on price axis = price zoom.
/// </summary>
public sealed class ChartInputController
{
    private double _wheelAccum;
    private bool _panning;
    private double _lastX;
    private double _lastY;
    private bool _priceAxisDragging;

    // Price-axis zoom session (absolute from Down — avoids stepwise jumps)
    private double _axisAnchorPrice;
    private double _axisStartMinT;
    private double _axisStartMaxT;
    private double _axisStartY;

    public int BarCount { get; set; }

    public ChartInputAction LastAction { get; private set; }

    public ChartInputAction OnPointer(ViewState vs, PointerInput input)
    {
        var region = InputHitTest.RegionAt(vs, input.X, input.Y);
        LastAction = ChartInputAction.None;

        if (input.Phase == PointerPhase.Wheel)
        {
            LastAction = HandleWheel(vs, input, region);
            return LastAction;
        }

        if (input.Phase == PointerPhase.DoubleClick)
        {
            if (region == HitRegion.PriceAxis)
            {
                PriceScaleEngine.SetAuto(vs.PriceScale);
                LastAction = ChartInputAction.FitAuto;
            }
            else if (region == HitRegion.Plot)
            {
                LastAction = ChartInputAction.OpenProperties;
            }
            return LastAction;
        }

        if (input.Phase == PointerPhase.Down)
        {
            _lastX = input.X;
            _lastY = input.Y;

            if (input.Button == PointerButton.Middle
                || (input.Button == PointerButton.Left && input.Space)
                || (input.Button == PointerButton.Left && region == HitRegion.Plot))
            {
                _panning = true;
                LastAction = input.Space ? ChartInputAction.TemporaryPan : ChartInputAction.Pan;
                return LastAction;
            }

            if (input.Button == PointerButton.Left && region == HitRegion.PriceAxis)
            {
                _priceAxisDragging = true;
                _axisStartY = input.Y;
                _axisStartMinT = vs.PriceScale.TransformPrice(vs.PriceScale.MinPrice);
                _axisStartMaxT = vs.PriceScale.TransformPrice(vs.PriceScale.MaxPrice);
                _axisAnchorPrice = new CoordinateConverter(vs, BarCount).Price(input.Y);
                LastAction = ChartInputAction.ManualPriceScale;
                return LastAction;
            }

            if (input.Button == PointerButton.Right)
            {
                LastAction = ChartInputAction.ContextMenu;
                return LastAction;
            }
        }

        if (input.Phase == PointerPhase.Move)
        {
            if (_panning)
            {
                double dx = input.X - _lastX;
                double dy = input.Y - _lastY;

                // Horizontal: content follows cursor
                if (Math.Abs(dx) >= 0.5)
                    PanEngine.PanHorizontal(vs, BarCount, -dx);

                // Vertical: shift price window so candles move with the cursor (4-direction pan)
                if (Math.Abs(dy) >= 0.5 && vs.PlotHeight > 0)
                {
                    double span = vs.PriceScale.MaxPrice - vs.PriceScale.MinPrice;
                    if (span > 0)
                    {
                        // Screen Y down → prices on screen move down → window shifts up in price
                        double dPrice = dy / vs.PlotHeight * span;
                        double min = vs.PriceScale.MinPrice + dPrice;
                        double max = vs.PriceScale.MaxPrice + dPrice;
                        PriceScaleEngine.SetManual(
                            vs.PriceScale,
                            vs.PriceScale.TransformPrice(min),
                            vs.PriceScale.TransformPrice(max));
                    }
                }

                _lastX = input.X;
                _lastY = input.Y;
                LastAction = ChartInputAction.Pan;
                return LastAction;
            }

            if (_priceAxisDragging)
            {
                // Smooth zoom from drag start: total dy from Down, not per-frame multiply
                double totalDy = input.Y - _axisStartY;
                // drag up (totalDy < 0) → zoom in (smaller span)
                double factor = Math.Clamp(1.0 + totalDy * 0.004, 0.25, 4.0);

                double aT = vs.PriceScale.TransformPrice(_axisAnchorPrice);
                double minT = aT + (_axisStartMinT - aT) * factor;
                double maxT = aT + (_axisStartMaxT - aT) * factor;
                if (maxT - minT < 1e-12)
                {
                    double mid = (maxT + minT) * 0.5;
                    minT = mid - 5e-13;
                    maxT = mid + 5e-13;
                }

                PriceScaleEngine.SetManual(vs.PriceScale, minT, maxT);
                LastAction = ChartInputAction.ManualPriceScale;
                return LastAction;
            }
        }

        if (input.Phase == PointerPhase.Up)
        {
            _panning = false;
            _priceAxisDragging = false;
        }

        return LastAction;
    }

    public ChartInputAction OnKey(ViewState vs, KeyInput key)
    {
        LastAction = key.Key switch
        {
            "Escape" => ChartInputAction.CancelTool,
            "Enter" => ChartInputAction.FinishDrawing,
            "Home" when key.Ctrl => ChartInputAction.LatestMarketPosition,
            "Home" => ChartInputAction.GoToLatest,
            "G" or "g" when key.Ctrl => ChartInputAction.NavigationDialog,
            _ => ChartInputAction.None
        };
        return LastAction;
    }

    private ChartInputAction HandleWheel(ViewState vs, PointerInput input, HitRegion region)
    {
        _wheelAccum += -input.WheelDelta / 120.0;
        int notches = (int)Math.Truncate(_wheelAccum);
        if (notches == 0)
            return ChartInputAction.None;
        _wheelAccum -= notches;

        if (region == HitRegion.PriceAxis)
        {
            double price = new CoordinateConverter(vs, BarCount).Price(input.Y);
            double span = vs.PriceScale.MaxPrice - vs.PriceScale.MinPrice;
            double newSpan = notches > 0
                ? span / Math.Pow(ZoomEngine.ZoomFactor(), notches)
                : span * Math.Pow(ZoomEngine.ZoomFactor(), -notches);

            double half = newSpan / 2;
            double min = price - half;
            double max = price + half;
            if (max <= min) max = min + 1e-8;
            PriceScaleEngine.SetManual(
                vs.PriceScale,
                vs.PriceScale.TransformPrice(min),
                vs.PriceScale.TransformPrice(max));
            return ChartInputAction.PriceZoom;
        }

        double zoomFactor = input.Ctrl
            ? ZoomEngine.PrecisionZoomFactor()
            : ZoomEngine.ZoomFactor();

        ZoomEngine.ZoomAt(vs, BarCount, input.X, zoomFactor, notches);
        return input.Ctrl ? ChartInputAction.PrecisionZoom : ChartInputAction.Zoom;
    }
}
