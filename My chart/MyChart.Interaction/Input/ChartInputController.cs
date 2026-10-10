using MyChart.Core.Models.Viewport;
using MyChart.Core.Scale;

namespace MyChart.Interaction.Input;

/// <summary>
/// T4.07 + C2/C3: left-drag pan on plot; Shift+Left = MultiSelect; wheel zoom at cursor.
/// </summary>
public sealed class ChartInputController
{
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

            // Shift+Left on plot = multi-select (T4.07); do not start pan
            if (input.Button == PointerButton.Left && region == HitRegion.Plot && input.Shift)
            {
                LastAction = ChartInputAction.MultiSelect;
                return LastAction;
            }

            // Left on plot = pan (owner C2)
            if (input.Button == PointerButton.Left && region == HitRegion.Plot)
            {
                _panning = true;
                LastAction = ChartInputAction.Pan;
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
                double totalDy = input.Y - _axisStartY;
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
        double notches = input.WheelDelta / 120.0;
        if (Math.Abs(notches) < 1e-9)
            return ChartInputAction.None;

        if (region == HitRegion.PriceAxis)
        {
            double midPrice = (vs.PriceScale.MinPrice + vs.PriceScale.MaxPrice) * 0.5;
            double factor = Math.Pow(ZoomEngine.ZoomFactor(), -notches);
            PriceScaleEngine.ZoomAroundPrice(vs.PriceScale, midPrice, factor);
            return ChartInputAction.PriceZoom;
        }

        if (vs.PlotWidth <= 1 || vs.BarSpacing <= 0 || BarCount <= 0)
            return ChartInputAction.None;

        double zoomFactor = input.Ctrl
            ? ZoomEngine.PrecisionZoomFactor()
            : ZoomEngine.ZoomFactor();
        ZoomEngine.ZoomAt(vs, BarCount, input.X, zoomFactor, notches);
        return input.Ctrl ? ChartInputAction.PrecisionZoom : ChartInputAction.Zoom;
    }
}
