using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MyChart.Core.Models.Market;
using MyChart.Core.Services;
using MyChart.Core.UI.Icons;
using MyChart.Core.UI.Toolbar;
using MyChart.UI.Icons;

namespace MyChart.App;

/// <summary>
/// Shell per System Map + mockup: TopToolbar, LeftToolbar, Chart, Tab, Status.
/// All 23 catalog icons via IconView + IconProvider (T7.01).
/// </summary>
public partial class MainWindow : Window
{
    private TopToolbarModel _top = TopToolbarModel.CreateDefault(settingsAvailable: true);
    private LeftToolbarModel _left = LeftToolbarModel.CreateDefault();
    private IconProvider? _icons;

    public MainWindow()
    {
        InitializeComponent();
        CompositionRoot.CreatePluginHost();
        _icons = CompositionRoot.Icons;
        WireSettingsToChart();
        BuildTopToolbar();
        BuildLeftToolbar();
        BuildContextToolbar();
    }

    private void WireSettingsToChart()
    {
        var host = CompositionRoot.SettingsHost;
        if (host is null) return;

        host.Bridge.Changed += _ =>
        {
            Dispatcher.Invoke(() => Chart.ApplySettings(host.Bridge.Values));
        };
        Chart.ApplySettings(host.Bridge.Values);
    }

    private static string IconKeyFor(TopToolbarItemKind kind) => kind switch
    {
        TopToolbarItemKind.Symbol => "Icon.Symbol",
        TopToolbarItemKind.Timeframe => "Icon.Timeframe",
        TopToolbarItemKind.ChartType => "Icon.ChartType",
        TopToolbarItemKind.Indicators => "Icon.Indicators",
        TopToolbarItemKind.Templates => "Icon.Templates",
        TopToolbarItemKind.Layout => "Icon.Layout",
        TopToolbarItemKind.Settings => "Icon.Settings",
        _ => "Icon.Settings"
    };

    private UIElement CreateIcon(string iconKey, IconVisualState state = IconVisualState.Normal)
    {
        var view = new IconView { IconKey = iconKey, VisualState = state };
        if (_icons is not null)
            view.Attach(_icons, ThemeService.Dark);
        return view;
    }

    private void BuildTopToolbar()
    {
        _top = CompositionRoot.CreateTopToolbar(symbol: "EURUSD", timeframe: Timeframe.M1);
        TopToolbarHost.Items.Clear();
        foreach (var item in _top.Items)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };
            panel.Children.Add(CreateIcon(IconKeyFor(item.Kind)));

            var text = item.SelectedValue is { Length: > 0 } sv ? sv : item.Label;
            panel.Children.Add(new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF)),
                FontSize = 11,
                Margin = new Thickness(6, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            });

            var btn = new Button
            {
                Content = panel,
                Style = (Style)FindResource("TbButton"),
                IsEnabled = item.IsEnabled,
                Tag = item,
                ToolTip = $"{item.Label} ({IconKeyFor(item.Kind)})"
            };
            btn.Click += OnTopToolbarClick;
            TopToolbarHost.Items.Add(btn);
        }
    }

    private void BuildLeftToolbar()
    {
        _left = LeftToolbarModel.CreateDefault();
        // Ensure first-party tools + core so all drawing icons appear
        LeftToolbarHost.Items.Clear();
        foreach (var item in _left.Items)
        {
            var iconKey = string.IsNullOrWhiteSpace(item.IconKey) ? "Icon.Cursor" : item.IconKey;
            var state = item.IsSelected ? IconVisualState.Active : IconVisualState.Normal;
            var icon = CreateIcon(iconKey, state);

            var btn = new Button
            {
                Content = icon,
                Style = (Style)FindResource("LeftTbButton"),
                Tag = item,
                ToolTip = item.ItemId,
                Background = item.IsSelected
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#505050")!)
                    : Brushes.Transparent
            };
            btn.Click += OnLeftToolbarClick;
            LeftToolbarHost.Items.Add(btn);
        }
    }


    private static readonly string[] ContextIconKeys =
    {
        "Icon.Color", "Icon.Width", "Icon.Opacity", "Icon.Style",
        "Icon.Template", "Icon.Lock", "Icon.Clone", "Icon.Delete",
    };

    private void BuildContextToolbar()
    {
        ContextToolbarHost.Items.Clear();
        foreach (var key in ContextIconKeys)
        {
            var btn = new Button
            {
                Content = CreateIcon(key),
                Style = (Style)FindResource("TbButton"),
                ToolTip = key,
                Tag = key
            };
            btn.Click += (_, _) => { StatusText.Text = key; };
            ContextToolbarHost.Items.Add(btn);
        }
    }

    private void OnTopToolbarClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: TopToolbarItem item }) return;

        if (item.Kind == TopToolbarItemKind.Settings)
        {
            OnSettingsClick(sender, e);
            return;
        }

        StatusText.Text = $"{item.Label}: {item.SelectedValue ?? item.Id}";
    }

    private void OnLeftToolbarClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: LeftToolbarItem item }) return;
        StatusText.Text = item.ItemId;
        _left = LeftToolbarModel.CreateDefault(selectedItemId: item.ItemId);
        BuildLeftToolbar();
    }

    private void OnTitleBarDrag(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2) { OnMaximize(sender, e); return; }
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }

    private void OnMinimize(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void OnMaximize(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void OnClose(object sender, RoutedEventArgs e) => Close();

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        var host = CompositionRoot.SettingsHost;
        if (host is null)
        {
            MessageBox.Show("SettingsHost is not initialized.", "MyChart");
            return;
        }
        try { host.ShowSettingsWindow(this); }
        catch (Exception ex) { MessageBox.Show("Failed to open Settings:\n" + ex.Message, "MyChart"); }
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var baseDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDir, "Fixtures"),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "MyChart.Tests", "Fixtures")),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "MyChart.Tests", "Fixtures")),
        };
        string? fixtures = candidates.FirstOrDefault(Directory.Exists);
        if (fixtures is null)
        {
            MessageBox.Show("Fixtures folder not found.");
            Chart.SetLoading(false);
            return;
        }
        try
        {
            await CompositionRoot.LoadFixturesAsync(Chart, fixtures);
            var host = CompositionRoot.SettingsHost;
            if (host is not null) Chart.ApplySettings(host.Bridge.Values);
            StatusText.Text = "EURUSD M1 · fixtures · icons " + (_icons?.All.Count ?? 0);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Failed to load chart data:\n" + ex.Message);
            Chart.SetLoading(false);
        }
    }
}
