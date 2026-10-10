using MyChart.Core.Models.Viewport;
using MyChart.Core.Scale;

namespace MyChart.Interaction.Input;

/// <summary>
/// T4.07 + C2 owner UX:
/// Zoom is always around the CENTER of the visible range (same effect from any mouse Y/X).
/// Price-axis wheel: up = expand (open), down = compress (close).
/// Plot drag = 4-direction pan.
/// </summary>
public sealed class ChartInputController
{
    private double _wheelAccum;
    private bool _panning;
    private double _lastX;
    private double _lastY;
    private bool _priceAxisDragging;

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
                // Zoom around CENTER of range at drag start — same result from any Y on the axis
                double totalDy = input.Y - _axisStartY;
                // drag down → expand; drag up → compress (matches wheel-up = open)
                double factor = Math.Clamp(Math.Exp(totalDy * 0.0035), 0.15, 8.0);
                double centerT = (_axisStartMinT + _axisStartMaxT) * 0.5;
                double half0 = (_axisStartMaxT - _axisStartMinT) * 0.5;
                double half = half0 * factor;
                if (half < 1e-12) half = 1e-12;
                PriceScaleEngine.SetManual(vs.PriceScale, centerT - half, centerT + half);
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
        // WPF: positive delta = wheel up. Owner: up = expand, down = compress on price axis.
        // notches > 0 after this mapping means "expand" for price; for time ZoomAt uses notches>0 = zoom in.
        _wheelAccum += input.WheelDelta / 120.0;
        int notches = (int)Math.Truncate(_wheelAccum);
        if (notches == 0)
            return ChartInputAction.None;
        _wheelAccum -= notches;

        if (region == HitRegion.PriceAxis)
        {
            // Uniform zoom around visible price CENTER (ignore cursor Y)
            double midPrice = (vs.PriceScale.MinPrice + vs.PriceScale.MaxPrice) * 0.5;
            // notches > 0 (wheel up) → factor > 1 → expand; notches < 0 → compress
            double factor = notches > 0
                ? Math.Pow(ZoomEngine.ZoomFactor(), notches)
                : Math.Pow(1.0 / ZoomEngine.ZoomFactor(), -notches);
            PriceScaleEngine.ZoomAroundPrice(vs.PriceScale, midPrice, factor);
            return ChartInputAction.PriceZoom;
        }

        // Plot / time axis: zoom around horizontal CENTER of plot (ignore cursor X)
        double zoomFactor = input.Ctrl
            ? ZoomEngine.PrecisionZoomFactor()
            : ZoomEngine.ZoomFactor();
        double centerX = vs.PlotLeft + vs.PlotWidth * 0.5;
        // Wheel up (notches>0) → expand time (zoom out) = fewer pixels per bar = smaller BarSpacing
        // ZoomAt: notches>0 multiplies BarSpacing → zoom in. So pass -notches for owner feel.
        ZoomEngine.ZoomAt(vs, BarCount, centerX, zoomFactor, -notches);
        return input.Ctrl ? ChartInputAction.PrecisionZoom : ChartInputAction.Zoom;
    }
}
