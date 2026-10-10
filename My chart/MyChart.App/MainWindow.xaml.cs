using System.IO;
using System.Windows;

namespace MyChart.App;

/// <summary>T4.08 thin shell — code-behind kept small; wiring in CompositionRoot.</summary>
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

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        var host = CompositionRoot.SettingsHost;
        if (host is null)
        {
            MessageBox.Show(
                "SettingsHost is not initialized.",
                "MyChart");
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
            // Re-apply after bars loaded so visible-candles/shift can take effect
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
