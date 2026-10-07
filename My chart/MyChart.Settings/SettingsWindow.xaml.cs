using System.ComponentModel;
using System.Windows;
using MyChart.Settings.Services;
using MyChart.Settings.ViewModels;

namespace MyChart.Settings;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;
    private readonly SettingsController _controller;

    public SettingsWindow()
    {
        try
        {
            InitializeComponent();
            _controller = new SettingsController(new SettingsProtocol());
            _viewModel = new SettingsViewModel(_controller);
            DataContext = _viewModel;
            Loaded += (_, _) => _viewModel.AttachUi(new WpfSettingsUi(this));
        }
        catch (Exception ex)
        {
            // R1: panel is a library — no App.xaml; surface error without depending on a host App class.
            MessageBox.Show(
                $"SettingsWindow failed to initialize:\n{ex.Message}",
                "MyChart Settings",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            throw;
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        _controller.DiscardChanges();
        base.OnClosing(e);
    }
}
