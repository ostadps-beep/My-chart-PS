using System.Windows;
using System.Windows.Controls;

namespace MyChart.Settings.Controls;

public partial class NumericField : UserControl
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(int),
        typeof(NumericField),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnChanged));

    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum), typeof(double), typeof(NumericField), new PropertyMetadata(0d));

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double), typeof(NumericField), new PropertyMetadata(100d));

    public static readonly DependencyProperty IncrementProperty = DependencyProperty.Register(
        nameof(Increment), typeof(double), typeof(NumericField), new PropertyMetadata(1d));

    public NumericField()
    {
        InitializeComponent();
        ValueBox.Text = Value.ToString();
    }

    public int Value
    {
        get => (int)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double Increment
    {
        get => (double)GetValue(IncrementProperty);
        set => SetValue(IncrementProperty, value);
    }

    private void Up_Click(object sender, RoutedEventArgs e) => Nudge((int)Math.Max(1, Increment));

    private void Down_Click(object sender, RoutedEventArgs e) => Nudge(-(int)Math.Max(1, Increment));

    private void ValueBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(ValueBox.Text, out var parsed))
            Value = Clamp(parsed);
        else
            ValueBox.Text = Value.ToString();
    }

    private void Nudge(int delta) => Value = Clamp(Value + delta);

    private int Clamp(int value) =>
        (int)Math.Clamp(value, Minimum, Maximum);

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (NumericField)d;
        if (control.ValueBox is not null)
            control.ValueBox.Text = control.Value.ToString();
    }
}

