using System.Windows;
using MyChart.Core.Models.Settings;
using MyChart.Settings;

namespace MyChart.App.Integration;

/// <summary>
/// T6.08 App host — only App file that references MyChart.Settings panel (R2).
/// Forwards protocol messages to Core SettingsBridge and opens SettingsWindow.
/// </summary>
public sealed class SettingsBridgeHost : ISettingsProtocolListener
{
    private readonly SettingsBridge _bridge;
    private SettingsWindow? _openWindow;

    public SettingsBridgeHost(SettingsBridge bridge) => _bridge = bridge;

    public SettingsBridge Bridge => _bridge;

    public void OnUpdate(SettingsUpdateMessage message)
        => _bridge.OnUpdate(message.Key, message.Value);

    public void OnApplied(SettingsAppliedMessage message)
        => _bridge.OnApplied(message.Changed);

    public void OnSave(SettingsSaveMessage message)
        => _bridge.OnSave(message.Path);

    /// <summary>R9 — open the integrated SettingsWindow.</summary>
    public void ShowSettingsWindow(Window? owner)
    {
        if (_openWindow is { IsLoaded: true })
        {
            _openWindow.Activate();
            _openWindow.Focus();
            return;
        }

        var window = new SettingsWindow();
        if (owner is not null)
            window.Owner = owner;

        window.Closed += (_, _) =>
        {
            if (ReferenceEquals(_openWindow, window))
                _openWindow = null;
        };

        _openWindow = window;
        // Modal so it is obvious the panel opened (visual VERIFY).
        window.ShowDialog();
    }
}
