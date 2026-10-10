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
        : this(hostListener: null)
    {
    }

    /// <summary>Host wires protocol so SETTINGS.UPDATE reaches Core SettingsBridge (visual item b).</summary>
    public SettingsWindow(ISettingsProtocolListener? hostListener)
    {
        try
        {
            InitializeComponent();
            var protocol = new SettingsProtocol();
            if (hostListener is not null)
                protocol.Subscribe(hostListener);
            _controller = new SettingsController(protocol);
            _viewModel = new SettingsViewModel(_controller);
            DataContext = _viewModel;
            Loaded += (_, _) => _viewModel.AttachUi(new WpfSettingsUi(this));
        }
        catch (Exception ex)
        {
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
