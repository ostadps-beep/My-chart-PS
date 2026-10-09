using System.ComponentModel;
using System.Windows;
using ChartMy.Services;
using ChartMy.ViewModels;

namespace ChartMy;

public partial class MainWindow : Window
{
    private readonly SettingsViewModel _viewModel;
    private readonly SettingsController _controller;

    public MainWindow()
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
            App.ShowException("MainWindow constructor", ex);
            throw;
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        _controller.DiscardChanges();
        base.OnClosing(e);
    }
}
