using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using MyChart.Core.Models.Market;
using MyChart.Core.UI.Toolbar;

namespace MyChart.App;

/// <summary>
/// Shell per System Map + mockup: TopToolbar, LeftToolbar, Chart, Tab, Status.
/// Models from Core (T6.03/T6.04); no hardcoded tool names on left (IconKey/CommandRef only).
/// </summary>
public partial class MainWindow : Window
{
    private TopToolbarModel _top = TopToolbarModel.CreateDefault(settingsAvailable: true);
    private LeftToolbarModel _left = LeftToolbarModel.CreateDefault();

    public MainWindow()
    {
        InitializeComponent();
        CompositionRoot.CreatePluginHost();
        WireSettingsToChart();
        BuildTopToolbar();
        BuildLeftToolbar();
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

    private void BuildTopToolbar()
    {
        _top = CompositionRoot.CreateTopToolbar(symbol: "EURUSD", timeframe: Timeframe.M1);
        TopToolbarHost.Items.Clear();
        foreach (var item in _top.Items)
        {
            var label = item.SelectedValue is { Length: > 0 } sv
                ? $"{item.Label}: {sv}"
                : item.Label;
            var btn = new Button
            {
                Content = label,
                Style = (Style)FindResource("TbButton"),
                IsEnabled = item.IsEnabled,
                Tag = item,
                ToolTip = item.Kind.ToString()
            };
            btn.Click += OnTopToolbarClick;
            TopToolbarHost.Items.Add(btn);
        }
    }

    private void BuildLeftToolbar()
    {
        _left = LeftToolbarModel.CreateDefault();
        LeftToolbarHost.Items.Clear();
        foreach (var item in _left.Items)
        {
            // Display: short id token only — no hard-coded drawing tool product names in UI text
            var caption = item.Kind switch
            {
                LeftToolbarItemKind.Cursor => "↖",
                LeftToolbarItemKind.Crosshair => "+",
                _ => "·"
            };
            var btn = new Button
            {
                Content = caption,
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
        // Refresh selection highlight via rebuild from model selection id
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
            StatusText.Text = "EURUSD M1 · fixtures";
        }
        catch (Exception ex)
        {
            MessageBox.Show("Failed to load chart data:\n" + ex.Message);
            Chart.SetLoading(false);
        }
    }
}
