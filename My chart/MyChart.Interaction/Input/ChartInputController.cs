using MyChart.Core.Models.Viewport;
using MyChart.Core.Scale;

namespace MyChart.Interaction.Input;

/// <summary>
/// T4.07 + C2 (CORRECTIONS_2026-10-10):
/// Wheel on plot = time zoom AT CURSOR (C2.8); price axis = Manual scale;
/// never SetAuto from zoom/pan (only double-click price axis).
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
                // Only explicit user reset → SetAuto (C2.2)
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
        // Spec factor 1.10 per notch; apply fractional for smoothness
        double notches = input.WheelDelta / 120.0;
        if (Math.Abs(notches) < 1e-9)
            return ChartInputAction.None;

        if (region == HitRegion.PriceAxis)
        {
            // Manual price zoom around range center (stable, not overwritten — C2.2)
            double midPrice = (vs.PriceScale.MinPrice + vs.PriceScale.MaxPrice) * 0.5;
            // wheel up (positive delta) → expand (owner direction last confirmed)
            double factor = Math.Pow(ZoomEngine.ZoomFactor(), -notches);
            PriceScaleEngine.ZoomAroundPrice(vs.PriceScale, midPrice, factor);
            return ChartInputAction.PriceZoom;
        }

        if (vs.PlotWidth <= 1 || vs.BarSpacing <= 0 || BarCount <= 0)
            return ChartInputAction.None;

        // C2.8: time zoom AT CURSOR (not center / not right edge)
        double zoomFactor = input.Ctrl
            ? ZoomEngine.PrecisionZoomFactor()
            : ZoomEngine.ZoomFactor();
        // ZoomAt: notches>0 = zoom in. WPF positive delta = wheel up → zoom in
        ZoomEngine.ZoomAt(vs, BarCount, input.X, zoomFactor, notches);
        return input.Ctrl ? ChartInputAction.PrecisionZoom : ChartInputAction.Zoom;
    }
}
