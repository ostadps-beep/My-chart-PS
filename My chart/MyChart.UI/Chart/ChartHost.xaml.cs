using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MyChart.Core.Analysis;
using MyChart.Core.Models.Chart;
using MyChart.Core.Models.Market;
using MyChart.Core.Models.Settings;
using MyChart.Core.Models.Viewport;
using MyChart.Core.Rendering;
using MyChart.Core.Scale;
using MyChart.Core.Services;
using MyChart.Interaction.Input;
using MyChart.Rendering.Layers;
using MyChart.Rendering.Skia;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace MyChart.UI.Chart;

/// <summary>
/// T4.08 ChartHost — AFTER T7 visual (a): free-move crosshair, HUD, readable time labels, startup ~200 bars.
/// Wires existing CrosshairCalculator/Renderer, HudDataProvider/HudRenderer, StartupView, RenderWindow.
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
    private readonly CrosshairRenderer _crosshair;
    private readonly HudRenderer _hud;

    private ViewState _view = new();
    private IReadOnlyList<Candle> _bars = Array.Empty<Candle>();
    private CoordinateConverter? _converter;
    private TimeIndexMapper? _timeMapper;
    private double _dpi = 1.0;
    private bool _loaded;
    private double _mouseX;
    private double _mouseY;
    private bool _mouseInSurface;
    private DateTimeOffset _lastPointerUtc = DateTimeOffset.UtcNow;
    private readonly SymbolInfo _symbol = new("EURUSD", "csv", SymbolGroup.Forex, 5);
    private Timeframe _timeframe = Timeframe.M1;

    public ChartHost()
    {
        InitializeComponent();
        _background = new BackgroundRenderer(_theme);
        _grid = new GridRenderer(_theme);
        _priceAxis = new PriceAxisRenderer(_theme);
        _timeAxis = new TimeAxisRenderer(_theme);
        _candles = new CandleRenderer(_theme);
        _currentPrice = new CurrentPriceRenderer(_theme);
        _crosshair = new CrosshairRenderer(_theme);
        _hud = new HudRenderer { Theme = _theme.Current };

        Focusable = true;
        Loaded += (_, _) => Focus();
        KeyDown += OnKeyDown;
        MouseLeave += (_, _) =>
        {
            _mouseInSurface = false;
            SkiaSurface.InvalidateVisual();
        };
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

    public void SetTimeframe(Timeframe tf) => _timeframe = tf;

    /// <summary>Visual (b) — Settings → ThemeService + chart invalidate (live).</summary>
    public void ApplySettings(ChartSettingValues values)
    {
        ArgumentNullException.ThrowIfNull(values);
        _theme.ApplyFromSettings(values);
        _hud.Theme = _theme.Current;
        _hud.Position = string.IsNullOrWhiteSpace(values.HudPosition) ? "TopLeft" : values.HudPosition;
        _hud.Transparency = values.HudTransparency;

        if (_bars.Count > 0 && values.VisibleCandles > 0)
        {
            int vis = Math.Min(Math.Max(20, values.VisibleCandles), _bars.Count);
            vis = Math.Min(vis, 300);
            StartupView.Apply(_view, vis, values.Shift);
            FitPriceToVisible();
            _converter = new CoordinateConverter(_view, _bars.Count);
        }

        SkiaSurface.InvalidateVisual();
    }


    private void ApplyStartupView()
    {
        if (_bars.Count == 0) return;
        UpdateSize();
        // AFTER T7 notes: sensible startup ≈ last 200 bars (existing StartupView)
        StartupView.Apply(_view, visibleCandles: Math.Min(200, Math.Max(20, _bars.Count)), shift: true);
        FitPriceToVisible();
        _converter = new CoordinateConverter(_view, _bars.Count);
    }

    private void FitPriceToVisible()
    {
        if (_bars.Count == 0) return;
        _converter = new CoordinateConverter(_view, _bars.Count);
        var range = RenderWindow.Compute(_converter);
        if (range.VisibleBarCount <= 0)
        {
            double minAll = _bars.Min(c => c.Low);
            double maxAll = _bars.Max(c => c.High);
            double padAll = (maxAll - minAll) * 0.05;
            if (padAll <= 0) padAll = 0.0001;
            _view.PriceScale.MinPrice = minAll - padAll;
            _view.PriceScale.MaxPrice = maxAll + padAll;
            return;
        }

        int from = Math.Clamp(range.From, 0, _bars.Count - 1);
        int to = Math.Clamp(range.To, from, _bars.Count - 1);
        double min = double.MaxValue, max = double.MinValue;
        for (int i = from; i <= to; i++)
        {
            if (_bars[i].Low < min) min = _bars[i].Low;
            if (_bars[i].High > max) max = _bars[i].High;
        }
        if (min > max) return;
        double pad = (max - min) * 0.05;
        if (pad <= 0) pad = 0.0001;
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
        _view.TimeAxisHeight = 28;
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

        FitPriceToVisible();
        _converter = new CoordinateConverter(_view, _bars.Count);

        var minT = _view.PriceScale.TransformPrice(_view.PriceScale.MinPrice);
        var maxT = _view.PriceScale.TransformPrice(_view.PriceScale.MaxPrice);
        var step = NiceTicks.ComputeStep(maxT - minT, _view.PlotHeight, 0.00001);
        var ticks = NiceTicks.PriceTicks(minT, maxT, step);

        var range = RenderWindow.Compute(_converter);
        var timeUs = new List<double>();
        var timeLabels = new List<(double, string)>();
        BuildReadableTimeLabels(range, timeUs, timeLabels);

        int first = Math.Max(0, range.RenderFrom);
        int lastIdx = Math.Min(_bars.Count - 1, range.RenderTo);
        var slice = first <= lastIdx
            ? _bars.Skip(first).Take(lastIdx - first + 1).ToList()
            : _bars;

        _grid.Render(ctx, _view, _converter, ticks, timeUs, _dpi);
        _candles.Render(ctx, slice, firstIndex: first, _converter, _dpi, digits: _symbol.Digits);
        _priceAxis.Render(ctx, _view, _converter, ticks, step, digits: _symbol.Digits, _dpi);
        _timeAxis.Render(ctx, _view, _converter, timeLabels, _dpi);

        var last = _bars[^1];
        bool bull = last.Close >= last.Open;
        _currentPrice.Render(ctx, _view, _converter, last.Close, bull, null, _symbol.Digits, _dpi);

        CrosshairState? crosshairState = null;
        if (_mouseInSurface && _timeMapper is not null)
        {
            crosshairState = CrosshairCalculator.Compute(
                _mouseX, _mouseY, _converter, _timeMapper, _bars, _symbol, _timeframe,
                new CrosshairFlags(Analysis: false, DataInspector: true, Magnet: false));
            _crosshair.Render(ctx, _view, crosshairState, _dpi);
        }

        // HUD (existing HudDataProvider + HudRenderer)
        var hudInput = new HudInput(
            Symbol: _symbol,
            Timeframe: _timeframe,
            Bars: _bars,
            Range: range,
            BarSpacing: _view.BarSpacing,
            PlotWidth: _view.PlotWidth,
            Crosshair: crosshairState,
            LastTick: null,
            ServerNow: DateTimeOffset.UtcNow,
            ReplayActive: false,
            ProviderConnected: true,
            ProviderName: "csv",
            CacheStatus: HudCacheStatus.Memory,
            Fps: 0,
            EnabledAdvanced: HudField.None);
        _hud.State = HudDataProvider.Compute(hudInput);
        _hud.Theme = _theme.Current;
        _hud.IsIdle = HudRenderer.ShouldIdle(DateTimeOffset.UtcNow - _lastPointerUtc);
        _hud.Render(ctx);
    }

    private void BuildReadableTimeLabels(
        RenderWindowRange range,
        List<double> timeUs,
        List<(double, string)> timeLabels)
    {
        if (_bars.Count == 0 || range.VisibleBarCount <= 0) return;
        int from = Math.Clamp(range.From, 0, _bars.Count - 1);
        int to = Math.Clamp(range.To, from, _bars.Count - 1);
        int span = Math.Max(1, to - from);
        // ~6–8 labels across the visible window
        int step = Math.Max(1, span / 7);
        string prevDay = "";
        for (int i = from; i <= to; i += step)
        {
            timeUs.Add(i);
            var ts = _bars[i].Timestamp.ToUniversalTime();
            string day = ts.ToString("MM-dd");
            string label = day != prevDay
                ? ts.ToString("MM-dd HH:mm")
                : ts.ToString("HH:mm");
            prevDay = day;
            timeLabels.Add((i, label));
        }
        // always include last visible bar
        if (to != from && (timeUs.Count == 0 || timeUs[^1] != to))
        {
            timeUs.Add(to);
            timeLabels.Add((to, _bars[to].Timestamp.ToUniversalTime().ToString("HH:mm")));
        }
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (_converter is null) return;
        var p = e.GetPosition(SkiaSurface);
        NotePointer(p.X, p.Y);
        _input.OnPointer(_view, new PointerInput(
            PointerPhase.Wheel, PointerButton.None, p.X, p.Y, e.Delta,
            Keyboard.Modifiers.HasFlag(ModifierKeys.Control),
            Keyboard.Modifiers.HasFlag(ModifierKeys.Shift),
            Keyboard.IsKeyDown(Key.Space)));
        e.Handled = true;
        FitPriceToVisible();
        SkiaSurface.InvalidateVisual();
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        Focus();
        var p = e.GetPosition(SkiaSurface);
        NotePointer(p.X, p.Y);
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
        NotePointer(p.X, p.Y);
        _input.OnPointer(_view, new PointerInput(
            PointerPhase.Up, PointerButton.Left, p.X, p.Y, 0, false, false, false));
        SkiaSurface.InvalidateVisual();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        var p = e.GetPosition(SkiaSurface);
        NotePointer(p.X, p.Y);

        // Pan only while dragging; crosshair updates on every free move (AFTER T7 item a)
        bool dragging = e.MiddleButton == MouseButtonState.Pressed
                        || e.LeftButton == MouseButtonState.Pressed
                        || Keyboard.IsKeyDown(Key.Space);
        if (dragging)
        {
            _input.OnPointer(_view, new PointerInput(
                PointerPhase.Move, PointerButton.Middle, p.X, p.Y, 0, false, false,
                Keyboard.IsKeyDown(Key.Space)));
            FitPriceToVisible();
        }

        SkiaSurface.InvalidateVisual();
    }

    private void NotePointer(double x, double y)
    {
        _mouseX = x;
        _mouseY = y;
        _mouseInSurface = true;
        _lastPointerUtc = DateTimeOffset.UtcNow;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        var action = _input.OnKey(_view, new KeyInput(
            e.Key.ToString(),
            Keyboard.Modifiers.HasFlag(ModifierKeys.Control),
            Keyboard.Modifiers.HasFlag(ModifierKeys.Shift),
            Keyboard.Modifiers.HasFlag(ModifierKeys.Alt)));

        if (action == ChartInputAction.GoToLatest || action == ChartInputAction.LatestMarketPosition)
            LatestViewController.GoToLatest(_view);

        FitPriceToVisible();
        SkiaSurface.InvalidateVisual();
    }
}
