using MyChart.Core.Models.Viewport;
using MyChart.Core.Scale;

namespace MyChart.Interaction.Input;

/// <summary>
/// T4.07 InputBindings + owner C2 UX:
/// Wheel=Zoom; Middle or Left-drag on plot=Pan; Space+Left=TemporaryPan;
/// Price-axis Left-drag=Manual price zoom (not time pan); Home=GoToLatest.
/// </summary>
public sealed class ChartInputController
{
    private double _wheelAccum;
    private bool _panning;
    private double _lastX;
    private double _lastY;
    private bool _priceAxisDragging;

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

            // Middle = pan (spec)
            if (input.Button == PointerButton.Middle)
            {
                _panning = true;
                LastAction = ChartInputAction.Pan;
                return LastAction;
            }

            // Space+Left = temporary pan (spec)
            if (input.Button == PointerButton.Left && input.Space)
            {
                _panning = true;
                LastAction = ChartInputAction.TemporaryPan;
                return LastAction;
            }

            // Price axis left = manual price scale (zoom via vertical drag)
            if (input.Button == PointerButton.Left && region == HitRegion.PriceAxis)
            {
                _priceAxisDragging = true;
                LastAction = ChartInputAction.ManualPriceScale;
                return LastAction;
            }

            // Plot left-drag = pan (owner request C2; click without move can still select later)
            if (input.Button == PointerButton.Left && region == HitRegion.Plot)
            {
                _panning = true;
                LastAction = input.Shift ? ChartInputAction.MultiSelect : ChartInputAction.Pan;
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
                if (Math.Abs(dx) >= PanEngine.DragThresholdDip || Math.Abs(input.Y - _lastY) >= PanEngine.DragThresholdDip)
                {
                    PanEngine.PanHorizontal(vs, BarCount, dx);
                    LastAction = ChartInputAction.Pan;
                }
                _lastX = input.X;
                _lastY = input.Y;
                return LastAction;
            }

            if (_priceAxisDragging)
            {
                // Vertical drag on price axis: scale range around cursor (T2.07 ManualDragZoom)
                // — keeps candles framed better than pure shift of the window
                double dy = input.Y - _lastY;
                if (Math.Abs(dy) > 0.1)
                {
                    var cc = new CoordinateConverter(vs, BarCount);
                    double anchor = cc.Price(input.Y);
                    PriceScaleEngine.ManualDragZoom(vs.PriceScale, anchor, dy);
                    LastAction = ChartInputAction.ManualPriceScale;
                }
                _lastX = input.X;
                _lastY = input.Y;
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
        // Home = GoToLatestCandle (spec); Ctrl+Home = LatestMarketPosition
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
        _wheelAccum += input.WheelDelta / 120.0;
        int notches = (int)Math.Truncate(_wheelAccum);
        if (notches == 0)
            return ChartInputAction.None;
        _wheelAccum -= notches;

        if (region == HitRegion.PriceAxis)
        {
            double price = new CoordinateConverter(vs, BarCount).Price(input.Y);
            double span = vs.PriceScale.MaxPrice - vs.PriceScale.MinPrice;
            double newSpan;
            if (notches > 0)
                newSpan = span / Math.Pow(ZoomEngine.ZoomFactor(), notches);
            else
                newSpan = span * Math.Pow(ZoomEngine.ZoomFactor(), -notches);

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
