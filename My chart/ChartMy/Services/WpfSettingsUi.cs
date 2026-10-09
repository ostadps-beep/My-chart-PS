using System.Windows;
using Microsoft.Win32;

namespace ChartMy.Services;

public sealed class WpfSettingsUi : ISettingsUi
{
    private readonly Window _window;

    public WpfSettingsUi(Window window) => _window = window;

    public void CloseWindow() => _window.Close();

    public string? PickOpenJson(string title)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            DefaultExt = ".json"
        };
        return dialog.ShowDialog(_window) == true ? dialog.FileName : null;
    }

    public string? PickSaveJson(string title, string defaultName)
    {
        var dialog = new SaveFileDialog
        {
            Title = title,
            Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
            DefaultExt = ".json",
            FileName = defaultName
        };
        return dialog.ShowDialog(_window) == true ? dialog.FileName : null;
    }

    public bool Confirm(string title, string message) =>
        MessageBox.Show(_window, message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    public void ShowMessage(string title, string message) =>
        MessageBox.Show(_window, message, title, MessageBoxButton.OK, MessageBoxImage.Information);
}
