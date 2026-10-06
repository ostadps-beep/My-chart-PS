using MyChart.Core.Models.Viewport;
using MyChart.Core.Scale;

namespace MyChart.Interaction.Input;

/// <summary>
/// T4.07 InputBindings — maps pointer/key to chart actions and applies Zoom/Pan engines.
/// Wheel always zooms; middle button always pans; Esc always cancels.
/// </summary>
public sealed class ChartInputController
{
    private double _wheelAccum;
    private bool _panning;
    private double _lastX;
    private double _lastY;
    private bool _priceAxisDragging;
    private double _priceDragStartY;
    private double _priceDragStartMin;
    private double _priceDragStartMax;

    public int BarCount { get; set; }

    /// <summary>Last action for tests / host feedback.</summary>
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
                LastAction = ChartInputAction.OpenProperties; // host decides drawing vs empty
            }
            return LastAction;
        }

        if (input.Phase == PointerPhase.Down)
        {
            _lastX = input.X;
            _lastY = input.Y;

            if (input.Button == PointerButton.Middle)
            {
                _panning = true;
                LastAction = ChartInputAction.Pan;
                return LastAction;
            }

            if (input.Button == PointerButton.Left && input.Space)
            {
                _panning = true;
                LastAction = ChartInputAction.TemporaryPan;
                return LastAction;
            }

            if (input.Button == PointerButton.Left && region == HitRegion.PriceAxis)
            {
                _priceAxisDragging = true;
                _priceDragStartY = input.Y;
                _priceDragStartMin = vs.PriceScale.MinPrice;
                _priceDragStartMax = vs.PriceScale.MaxPrice;
                LastAction = ChartInputAction.ManualPriceScale;
                return LastAction;
            }

            if (input.Button == PointerButton.Left && region == HitRegion.Plot)
            {
                LastAction = input.Shift ? ChartInputAction.MultiSelect : ChartInputAction.Select;
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
                if (Math.Abs(dx) >= PanEngine.DragThresholdDip || true)
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
                // Vertical drag: shift visible price window
                double dy = input.Y - _priceDragStartY;
                double span = _priceDragStartMax - _priceDragStartMin;
                if (span > 0 && vs.PlotHeight > 0)
                {
                    double dPrice = dy / vs.PlotHeight * span;
                    double min = _priceDragStartMin + dPrice;
                    double max = _priceDragStartMax + dPrice;
                    var minT = vs.PriceScale.TransformPrice(min);
                    var maxT = vs.PriceScale.TransformPrice(max);
                    PriceScaleEngine.SetManual(vs.PriceScale, minT, maxT);
                    LastAction = ChartInputAction.ManualPriceScale;
                }
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
        // notch = delta / 120; fractional accumulate
        _wheelAccum += input.WheelDelta / 120.0;
        int notches = (int)Math.Truncate(_wheelAccum);
        if (notches == 0)
            return ChartInputAction.None;
        _wheelAccum -= notches;

        if (region == HitRegion.PriceAxis)
        {
            // PriceZoom: scale price range around cursor Y
            double factor = ZoomEngine.ZoomFactor();
            if (notches < 0) factor = 1.0 / ZoomEngine.ZoomFactor();
            double price = new CoordinateConverter(vs, BarCount).Price(input.Y);
            double span = vs.PriceScale.MaxPrice - vs.PriceScale.MinPrice;
            double newSpan = span / Math.Pow(factor, Math.Abs(notches));
            if (notches < 0) newSpan = span * Math.Pow(ZoomEngine.ZoomFactor(), Math.Abs(notches));
            // zoom in (notches>0) => smaller span
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

        // Plot or time axis: time zoom
        double zoomFactor = input.Ctrl
            ? ZoomEngine.PrecisionZoomFactor()
            : ZoomEngine.ZoomFactor();

        // notches > 0 = zoom in
        ZoomEngine.ZoomAt(vs, BarCount, input.X, zoomFactor, notches);
        return input.Ctrl ? ChartInputAction.PrecisionZoom : ChartInputAction.Zoom;
    }
}
