using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ChartMy.Controls;

public partial class ToggleSwitch : UserControl
{
    public static readonly DependencyProperty IsOnProperty = DependencyProperty.Register(
        nameof(IsOn),
        typeof(bool),
        typeof(ToggleSwitch),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnChanged));

    public ToggleSwitch()
    {
        InitializeComponent();
        UpdateVisual();
    }

    public bool IsOn
    {
        get => (bool)GetValue(IsOnProperty);
        set => SetValue(IsOnProperty, value);
    }

    private void Root_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!IsEnabled)
            return;
        IsOn = !IsOn;
    }

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((ToggleSwitch)d).UpdateVisual();

    private void UpdateVisual()
    {
        if (Track is null || Thumb is null || StateText is null)
            return;

        Track.Background = new SolidColorBrush(IsOn
            ? Color.FromRgb(0x2E, 0x7D, 0x32)
            : Color.FromRgb(0x61, 0x61, 0x61));
        Thumb.HorizontalAlignment = IsOn ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        StateText.Text = IsOn ? "ON" : "OFF";
    }
}
