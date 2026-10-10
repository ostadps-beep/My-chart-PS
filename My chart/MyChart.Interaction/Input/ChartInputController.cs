using MyChart.Core.Models.Viewport;
using MyChart.Core.Scale;

namespace MyChart.Interaction.Input;

/// <summary>
/// T4.07 + C2: 4-dir plot pan; price-axis zoom keeps price under cursor fixed (no downward jump).
/// </summary>
public sealed class ChartInputController
{
    private double _wheelAccum;
    private bool _panning;
    private double _lastX;
    private double _lastY;
    private bool _priceAxisDragging;

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
                // Anchor = price at cursor — must stay on same screen row while zooming
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

                if (Math.Abs(dx) >= 0.5)
                    PanEngine.PanHorizontal(vs, BarCount, -dx);

                if (Math.Abs(dy) >= 0.5 && vs.PlotHeight > 0)
                {
                    double span = vs.PriceScale.MaxPrice - vs.PriceScale.MinPrice;
                    if (span > 0)
                    {
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
                // Absolute zoom from Down: factor from total vertical drag (smooth, no step-stack)
                // drag up (totalDy < 0) → factor < 1 → zoom in; anchor price fixed
                double totalDy = input.Y - _axisStartY;
                double factor = Math.Clamp(Math.Exp(totalDy * 0.0035), 0.15, 8.0);

                double aT = vs.PriceScale.TransformPrice(_axisAnchorPrice);
                double minT = aT + (_axisStartMinT - aT) * factor;
                double maxT = aT + (_axisStartMaxT - aT) * factor;
                if (maxT <= minT)
                {
                    double mid = (maxT + minT) * 0.5;
                    minT = mid - 1e-10;
                    maxT = mid + 1e-10;
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
            // Keep price under cursor fixed (do NOT re-center the range on cursor)
            double anchor = new CoordinateConverter(vs, BarCount).Price(input.Y);
            double factor = notches > 0
                ? Math.Pow(1.0 / ZoomEngine.ZoomFactor(), notches)  // zoom in
                : Math.Pow(ZoomEngine.ZoomFactor(), -notches);       // zoom out
            PriceScaleEngine.ZoomAroundPrice(vs.PriceScale, anchor, factor);
            return ChartInputAction.PriceZoom;
        }

        double zoomFactor = input.Ctrl
            ? ZoomEngine.PrecisionZoomFactor()
            : ZoomEngine.ZoomFactor();

        ZoomEngine.ZoomAt(vs, BarCount, input.X, zoomFactor, notches);
        return input.Ctrl ? ChartInputAction.PrecisionZoom : ChartInputAction.Zoom;
    }
}
