using System.IO;
using System.Windows;
using System.Windows.Input;

namespace MyChart.App;

/// <summary>Chrome #1E1E1E, custom title bar, menu, Settings right; Settings live wiring.</summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        CompositionRoot.CreatePluginHost();
        WireSettingsToChart();
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

    private void OnTitleBarDrag(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            OnMaximize(sender, e);
            return;
        }
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void OnMinimize(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void OnMaximize(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void OnClose(object sender, RoutedEventArgs e) => Close();

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        var host = CompositionRoot.SettingsHost;
        if (host is null)
        {
            MessageBox.Show("SettingsHost is not initialized.", "MyChart");
            return;
        }

        try
        {
            host.ShowSettingsWindow(this);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Failed to open Settings:\n" + ex.Message, "MyChart");
        }
    }

    private void OnExitClick(object sender, RoutedEventArgs e) => Close();

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
            MessageBox.Show("Fixtures folder not found. Place EURUSD_M1.csv under Fixtures/.");
            Chart.SetLoading(false);
            return;
        }

        try
        {
            await CompositionRoot.LoadFixturesAsync(Chart, fixtures);
            var host = CompositionRoot.SettingsHost;
            if (host is not null)
                Chart.ApplySettings(host.Bridge.Values);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Failed to load chart data:\n" + ex.Message);
            Chart.SetLoading(false);
        }
    }
}
