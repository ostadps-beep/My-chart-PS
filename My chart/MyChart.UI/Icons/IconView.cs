using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using MyChart.Core.Models.Rendering;
using MyChart.Core.Services;
using MyChart.Core.UI.Icons;

namespace MyChart.UI.Icons;

/// <summary>
/// T7.01 monochrome icon control — Path fills from theme colour rules; size = ViewBox DIP.
/// Stroke-only icons use Stroke; filled icons use Fill.
/// </summary>
public sealed class IconView : Viewbox
{
    public static readonly DependencyProperty IconKeyProperty =
        DependencyProperty.Register(
            nameof(IconKey),
            typeof(string),
            typeof(IconView),
            new PropertyMetadata(null, OnVisualChanged));

    public static readonly DependencyProperty VisualStateProperty =
        DependencyProperty.Register(
            nameof(VisualState),
            typeof(IconVisualState),
            typeof(IconView),
            new PropertyMetadata(IconVisualState.Normal, OnVisualChanged));

    private readonly Path _path = new()
    {
        Stretch = Stretch.Uniform,
        StrokeThickness = 1.25,
        StrokeStartLineCap = PenLineCap.Round,
        StrokeEndLineCap = PenLineCap.Round,
        StrokeLineJoin = PenLineJoin.Round
    };

    private IconProvider? _provider;
    private ThemeTokens _theme = ThemeService.Dark;

    public IconView()
    {
        Child = _path;
        Width = IconSystemModel.IconViewBox;
        Height = IconSystemModel.IconViewBox;
        SnapsToDevicePixels = true;
    }

    public string? IconKey
    {
        get => (string?)GetValue(IconKeyProperty);
        set => SetValue(IconKeyProperty, value);
    }

    public IconVisualState VisualState
    {
        get => (IconVisualState)GetValue(VisualStateProperty);
        set => SetValue(VisualStateProperty, value);
    }

    public void Attach(IconProvider provider, ThemeTokens? theme = null)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        if (theme is not null)
            _theme = theme;
        Width = provider.ViewBox;
        Height = provider.ViewBox;
        Refresh();
    }

    public void SetTheme(ThemeTokens theme)
    {
        _theme = theme ?? throw new ArgumentNullException(nameof(theme));
        Refresh();
    }

    private static void OnVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is IconView view)
            view.Refresh();
    }

    private void Refresh()
    {
        if (_provider is null || string.IsNullOrWhiteSpace(IconKey))
        {
            _path.Data = null;
            return;
        }

        if (!_provider.TryGetGeometry(IconKey, out var geometry))
        {
            _path.Data = null;
            return;
        }

        _path.Data = geometry;
        var brush = _provider.BrushFor(_theme, VisualState);

        // Outline Fluent icons: prefer stroke; clear fill so open paths stay visible.
        _path.Fill = null;
        _path.Stroke = brush;
    }
}
