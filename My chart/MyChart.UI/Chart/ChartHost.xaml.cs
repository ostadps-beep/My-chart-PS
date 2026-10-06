using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MyChart.Core.Models.Market;
using MyChart.Core.Models.Viewport;
using MyChart.Core.Scale;
using MyChart.Core.Services;
using MyChart.Interaction.Input;
using MyChart.Rendering.Layers;
using MyChart.Rendering.Skia;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace MyChart.UI.Chart;

/// <summary>
/// T4.08 ChartHost — WPF UserControl hosting SKElement.
/// DPI from VisualTreeHelper; pointer/key → ChartInputController only.
/// </summary>
public partial class ChartHost : UserControl
{
    private readonly ThemeService _theme = new();
    private readonly ChartInputController _input = new();
    private readonly BackgroundRenderer _background;
    private readonly GridRenderer _grid;
    private readonly PriceAxisRenderer _priceAxis;
    private readonly TimeAxisRenderer _timeAxis;
    private readonly CandleRenderer _candles;
    private readonly CurrentPriceRenderer _currentPrice;

    private ViewState _view = new();
    private IReadOnlyList<Candle> _bars = Array.Empty<Candle>();
    private CoordinateConverter? _converter;
    private TimeIndexMapper? _timeMapper;
    private double _dpi = 1.0;
    private bool _loaded;

    public ChartHost()
    {
        InitializeComponent();
        _background = new BackgroundRenderer(_theme);
        _grid = new GridRenderer(_theme);
        _priceAxis = new PriceAxisRenderer(_theme);
        _timeAxis = new TimeAxisRenderer(_theme);
        _candles = new CandleRenderer(_theme);
        _currentPrice = new CurrentPriceRenderer(_theme);

        Focusable = true;
        Loaded += (_, _) => Focus();
        KeyDown += OnKeyDown;
    }

    public void SetLoading(bool loading)
        => LoadingOverlay.Visibility = loading ? Visibility.Visible : Visibility.Collapsed;

    public void SetData(IReadOnlyList<Candle> bars, TimeIndexMapper mapper)
    {
        _bars = bars;
        _timeMapper = mapper;
        _input.BarCount = bars.Count;
        _converter = new CoordinateConverter(_view, bars.Count);
        _loaded = true;
        SetLoading(false);
        ApplyStartupView();
        SkiaSurface.InvalidateVisual();
    }

    private void ApplyStartupView()
    {
        if (_bars.Count == 0) return;
        _view.BarSpacing = 8;
        LatestViewController.GoToLatest(_view);
        // Fit price to visible range roughly full series
        double min = _bars.Min(c => c.Low);
        double max = _bars.Max(c => c.High);
        double pad = (max - min) * 0.05;
        if (pad <= 0) pad = 0.0001;
        _view.PriceScale.MinPrice = min - pad;
        _view.PriceScale.MaxPrice = max + pad;
        PriceScaleEngine.SetAuto(_view.PriceScale);
        _view.PriceScale.MinPrice = min - pad;
        _view.PriceScale.MaxPrice = max + pad;
    }

    private void UpdateSize()
    {
        var dpi = VisualTreeHelper.GetDpi(this);
        _dpi = dpi.PixelsPerDip;
        _view.Width = ActualWidth > 0 ? ActualWidth : 800;
        _view.Height = ActualHeight > 0 ? ActualHeight : 500;
        _view.PriceAxisWidth = 64;
        _view.TimeAxisHeight = 24;
        if (_bars.Count > 0)
            _converter = new CoordinateConverter(_view, _bars.Count);
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        UpdateSize();
        var canvas = e.Surface.Canvas;
        int w = e.Info.Width;
        int h = e.Info.Height;
        using var ctx = new SkiaRenderContext(canvas, w, h, _dpi);

        _background.Render(ctx, _view, _dpi);

        if (!_loaded || _converter is null || _bars.Count == 0)
            return;

        var minT = _view.PriceScale.TransformPrice(_view.PriceScale.MinPrice);
        var maxT = _view.PriceScale.TransformPrice(_view.PriceScale.MaxPrice);
        var step = NiceTicks.ComputeStep(maxT - minT, _view.PlotHeight, 0.00001);
        var ticks = NiceTicks.PriceTicks(minT, maxT, step);

        // Simple time labels every ~10 bars
        var timeUs = new List<double>();
        var timeLabels = new List<(double, string)>();
        for (int i = 0; i < _bars.Count; i += Math.Max(1, _bars.Count / 8))
        {
            timeUs.Add(i);
            timeLabels.Add((i, _bars[i].Timestamp.ToString("HH:mm")));
        }

        _grid.Render(ctx, _view, _converter, ticks, timeUs, _dpi);
        _candles.Render(ctx, _bars, firstIndex: 0, _converter, _dpi, digits: 5);
        _priceAxis.Render(ctx, _view, _converter, ticks, step, digits: 5, _dpi);
        _timeAxis.Render(ctx, _view, _converter, timeLabels, _dpi);

        var last = _bars[^1];
        bool bull = last.Close >= last.Open;
        _currentPrice.Render(ctx, _view, _converter, last.Close, bull, null, 5, _dpi);
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (_converter is null) return;
        var p = e.GetPosition(SkiaSurface);
        _input.OnPointer(_view, new PointerInput(
            PointerPhase.Wheel, PointerButton.None, p.X, p.Y, e.Delta,
            Keyboard.Modifiers.HasFlag(ModifierKeys.Control),
            Keyboard.Modifiers.HasFlag(ModifierKeys.Shift),
            Keyboard.IsKeyDown(Key.Space)));
        e.Handled = true;
        SkiaSurface.InvalidateVisual();
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        Focus();
        var p = e.GetPosition(SkiaSurface);
        var btn = e.ChangedButton switch
        {
            MouseButton.Middle => PointerButton.Middle,
            MouseButton.Right => PointerButton.Right,
            _ => PointerButton.Left
        };
        var phase = e.ClickCount >= 2 ? PointerPhase.DoubleClick : PointerPhase.Down;
        _input.OnPointer(_view, new PointerInput(
            phase, btn, p.X, p.Y, 0,
            Keyboard.Modifiers.HasFlag(ModifierKeys.Control),
            Keyboard.Modifiers.HasFlag(ModifierKeys.Shift),
            Keyboard.IsKeyDown(Key.Space)));
        SkiaSurface.InvalidateVisual();
    }

    private void OnMouseLeftDown(object sender, MouseButtonEventArgs e)
    {
        // handled in OnMouseDown
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        var p = e.GetPosition(SkiaSurface);
        _input.OnPointer(_view, new PointerInput(
            PointerPhase.Up, PointerButton.Left, p.X, p.Y, 0, false, false, false));
        SkiaSurface.InvalidateVisual();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (e.MiddleButton != MouseButtonState.Pressed
            && e.LeftButton != MouseButtonState.Pressed)
            return;
        var p = e.GetPosition(SkiaSurface);
        _input.OnPointer(_view, new PointerInput(
            PointerPhase.Move, PointerButton.Middle, p.X, p.Y, 0, false, false,
            Keyboard.IsKeyDown(Key.Space)));
        SkiaSurface.InvalidateVisual();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        var action = _input.OnKey(_view, new KeyInput(
            e.Key.ToString(),
            Keyboard.Modifiers.HasFlag(ModifierKeys.Control),
            Keyboard.Modifiers.HasFlag(ModifierKeys.Shift),
            Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)));

        if (action == ChartInputAction.GoToLatest || action == ChartInputAction.LatestMarketPosition)
        {
            LatestViewController.GoToLatest(_view);
            e.Handled = true;
            SkiaSurface.InvalidateVisual();
        }
        else if (action == ChartInputAction.CancelTool)
        {
            e.Handled = true;
        }
    }
}
