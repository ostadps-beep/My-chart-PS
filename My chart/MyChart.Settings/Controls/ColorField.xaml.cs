using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MyChart.Settings.Controls;

public partial class ColorField : UserControl
{
    public static readonly DependencyProperty HexValueProperty = DependencyProperty.Register(
        nameof(HexValue),
        typeof(string),
        typeof(ColorField),
        new FrameworkPropertyMetadata("#FFFFFF", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnChanged));

    private bool _updating;
    private bool _svDragging;
    private bool _hueDragging;
    private double _hue;
    private double _sat = 1;
    private double _val = 1;
    private byte _alpha = 255;
    private bool _spectrumReady;

    public ColorField()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplyFromHex(HexValue, updateHex: false);
    }

    public string HexValue
    {
        get => (string)GetValue(HexValueProperty);
        set => SetValue(HexValueProperty, value);
    }

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((ColorField)d).ApplyFromHex((string?)e.NewValue, updateHex: false);

    private void PickerButton_Checked(object sender, RoutedEventArgs e)
    {
        EnsureSpectrum();
        SyncUi();
    }

    private void HexBox_LostFocus(object sender, RoutedEventArgs e) =>
        ApplyFromHex(HexBox.Text, updateHex: true);

    private void Channel_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updating)
            return;
        var color = Color.FromArgb(
            (byte)AlphaSlider.Value,
            (byte)RedSlider.Value,
            (byte)GreenSlider.Value,
            (byte)BlueSlider.Value);
        ColorToHsv(color, out _hue, out _sat, out _val);
        _alpha = color.A;
        Commit(color, renderSv: true);
    }

    private void SatSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updating)
            return;
        _sat = SatSlider.Value / 100.0;
        Commit(HsvToColor(_hue, _sat, _val, _alpha), renderSv: false);
    }

    private void BrightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updating)
            return;
        _val = BrightSlider.Value / 100.0;
        Commit(HsvToColor(_hue, _sat, _val, _alpha), renderSv: false);
    }

    private void SvHost_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _svDragging = true;
        SvHost.CaptureMouse();
        PickSv(e.GetPosition(SvHost));
    }

    private void SvHost_MouseMove(object sender, MouseEventArgs e)
    {
        if (_svDragging && e.LeftButton == MouseButtonState.Pressed)
            PickSv(e.GetPosition(SvHost));
    }

    private void SvHost_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _svDragging = false;
        SvHost.ReleaseMouseCapture();
    }

    private void HueHost_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _hueDragging = true;
        HueHost.CaptureMouse();
        PickHue(e.GetPosition(HueHost));
    }

    private void HueHost_MouseMove(object sender, MouseEventArgs e)
    {
        if (_hueDragging && e.LeftButton == MouseButtonState.Pressed)
            PickHue(e.GetPosition(HueHost));
    }

    private void HueHost_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _hueDragging = false;
        HueHost.ReleaseMouseCapture();
    }

    private void PickSv(Point point)
    {
        var w = Math.Max(1, SvHost.ActualWidth);
        var h = Math.Max(1, SvHost.ActualHeight);
        _sat = Math.Clamp(point.X / w, 0, 1);
        _val = Math.Clamp(1 - point.Y / h, 0, 1);
        Commit(HsvToColor(_hue, _sat, _val, _alpha), renderSv: false);
    }

    private void PickHue(Point point)
    {
        var w = Math.Max(1, HueHost.ActualWidth);
        _hue = Math.Clamp(point.X / w, 0, 1) * 360;
        Commit(HsvToColor(_hue, _sat, _val, _alpha), renderSv: true);
    }

    private void ApplyFromHex(string? value, bool updateHex)
    {
        if (!TryParse(Normalize(value), out var color))
            return;
        ColorToHsv(color, out _hue, out _sat, out _val);
        _alpha = color.A;
        Commit(color, renderSv: true, writeHex: updateHex);
    }

    private void Commit(Color color, bool renderSv, bool writeHex = true)
    {
        _alpha = color.A;
        if (writeHex)
        {
            var hex = FormatHex(color);
            if (!string.Equals(HexValue, hex, StringComparison.OrdinalIgnoreCase))
                HexValue = hex;
        }

        EnsureSpectrum();
        if (renderSv)
            RenderSv();
        SyncUi(color);
    }

    private void SyncUi(Color? color = null)
    {
        if (Swatch is null)
            return;

        color ??= HsvToColor(_hue, _sat, _val, _alpha);
        var brush = new SolidColorBrush(color.Value);
        Swatch.Background = brush;
        if (PreviewSwatch is not null)
            PreviewSwatch.Background = brush;
        if (HexBox is not null)
        {
            var hex = FormatHex(color.Value);
            if (HexBox.Text != hex)
                HexBox.Text = hex;
        }

        _updating = true;
        try
        {
            if (RedSlider is not null)
            {
                RedSlider.Value = color.Value.R;
                GreenSlider.Value = color.Value.G;
                BlueSlider.Value = color.Value.B;
                AlphaSlider.Value = color.Value.A;
            }

            if (SatSlider is not null)
            {
                SatSlider.Value = _sat * 100;
                BrightSlider.Value = _val * 100;
            }
        }
        finally
        {
            _updating = false;
        }

        UpdateMarkers();
    }

    private void UpdateMarkers()
    {
        if (SvHost is null || SvMarker is null || HueMarker is null)
            return;
        var svW = SvHost.ActualWidth;
        var svH = SvHost.ActualHeight;
        if (svW > 0 && svH > 0)
        {
            Canvas.SetLeft(SvMarker, _sat * svW - SvMarker.Width / 2);
            Canvas.SetTop(SvMarker, (1 - _val) * svH - SvMarker.Height / 2);
        }

        var hueW = HueHost.ActualWidth;
        if (hueW > 0)
            Canvas.SetLeft(HueMarker, (_hue / 360.0) * hueW - HueMarker.Width / 2);
    }

    private void EnsureSpectrum()
    {
        if (HueImage is null)
            return;
        if (!_spectrumReady)
        {
            RenderHue();
            _spectrumReady = true;
        }

        if (SvImage?.Source is null)
            RenderSv();
    }

    private void RenderHue()
    {
        const int width = 360;
        const int height = 1;
        var bmp = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
        var pixels = new byte[width * 4];
        for (var x = 0; x < width; x++)
        {
            var c = HsvToColor(x, 1, 1, 255);
            var i = x * 4;
            pixels[i] = c.B;
            pixels[i + 1] = c.G;
            pixels[i + 2] = c.R;
            pixels[i + 3] = 255;
        }

        bmp.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);
        HueImage.Source = bmp;
    }

    private void RenderSv()
    {
        if (SvImage is null)
            return;
        const int width = 160;
        const int height = 120;
        var bmp = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
        var pixels = new byte[width * height * 4];
        for (var y = 0; y < height; y++)
        {
            var v = 1.0 - y / (double)(height - 1);
            for (var x = 0; x < width; x++)
            {
                var s = x / (double)(width - 1);
                var c = HsvToColor(_hue, s, v, 255);
                var i = (y * width + x) * 4;
                pixels[i] = c.B;
                pixels[i + 1] = c.G;
                pixels[i + 2] = c.R;
                pixels[i + 3] = 255;
            }
        }

        bmp.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);
        SvImage.Source = bmp;
    }

    private static string FormatHex(Color color) =>
        color.A == 255
            ? $"#{color.R:X2}{color.G:X2}{color.B:X2}"
            : $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "#FFFFFF";
        value = value.Trim();
        if (!value.StartsWith('#'))
            value = "#" + value;
        return value.ToUpperInvariant();
    }

    private static bool TryParse(string hex, out Color color)
    {
        color = Colors.White;
        hex = hex.TrimStart('#');
        if (hex.Length == 6 &&
            byte.TryParse(hex[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r) &&
            byte.TryParse(hex[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g) &&
            byte.TryParse(hex[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
        {
            color = Color.FromRgb(r, g, b);
            return true;
        }

        if (hex.Length == 8 &&
            byte.TryParse(hex[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var a) &&
            byte.TryParse(hex[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r) &&
            byte.TryParse(hex[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g) &&
            byte.TryParse(hex[6..8], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b))
        {
            color = Color.FromArgb(a, r, g, b);
            return true;
        }

        return false;
    }

    private static void ColorToHsv(Color color, out double h, out double s, out double v)
    {
        var r = color.R / 255.0;
        var g = color.G / 255.0;
        var b = color.B / 255.0;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var delta = max - min;
        v = max;
        s = max <= 0 ? 0 : delta / max;
        if (delta <= 0)
        {
            h = 0;
            return;
        }

        if (Math.Abs(max - r) < double.Epsilon)
            h = 60 * (((g - b) / delta) % 6);
        else if (Math.Abs(max - g) < double.Epsilon)
            h = 60 * (((b - r) / delta) + 2);
        else
            h = 60 * (((r - g) / delta) + 4);

        if (h < 0)
            h += 360;
    }

    private static Color HsvToColor(double h, double s, double v, byte alpha)
    {
        s = Math.Clamp(s, 0, 1);
        v = Math.Clamp(v, 0, 1);
        h = (h % 360 + 360) % 360;
        var c = v * s;
        var x = c * (1 - Math.Abs(h / 60 % 2 - 1));
        var m = v - c;
        double r, g, b;
        if (h < 60) { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }

        return Color.FromArgb(
            alpha,
            (byte)Math.Round((r + m) * 255),
            (byte)Math.Round((g + m) * 255),
            (byte)Math.Round((b + m) * 255));
    }
}

