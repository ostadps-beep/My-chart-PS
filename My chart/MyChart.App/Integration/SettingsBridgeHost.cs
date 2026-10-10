using System.Windows;
using MyChart.Core.Models.Settings;
using MyChart.Settings;
using MyChart.Settings.Services;

namespace MyChart.App.Integration;

/// <summary>
/// T6.08 / visual (b) — App host for Settings panel; forwards protocol to Core SettingsBridge.
/// </summary>
public sealed class SettingsBridgeHost
{
    private readonly SettingsBridge _bridge;
    private readonly HostProtocolAdapter _adapter;
    private SettingsWindow? _openWindow;

    public SettingsBridgeHost(SettingsBridge bridge)
    {
        _bridge = bridge;
        _adapter = new HostProtocolAdapter(bridge);
    }

    public SettingsBridge Bridge => _bridge;

    public void ShowSettingsWindow(Window? owner)
    {
        if (_openWindow is { IsLoaded: true })
        {
            _openWindow.Activate();
            _openWindow.Focus();
            return;
        }

        var window = new SettingsWindow(_adapter);
        if (owner is not null)
            window.Owner = owner;

        window.Closed += (_, _) =>
        {
            if (ReferenceEquals(_openWindow, window))
                _openWindow = null;
        };

        _openWindow = window;
        window.ShowDialog();
    }

    private sealed class HostProtocolAdapter : ISettingsProtocolListener
    {
        private readonly SettingsBridge _bridge;

        public HostProtocolAdapter(SettingsBridge bridge) => _bridge = bridge;

        public void OnUpdate(SettingsUpdateMessage message)
            => _bridge.OnUpdate(message.Key, message.Value);

        public void OnApplied(SettingsAppliedMessage message)
            => _bridge.OnApplied(message.Changed);

        public void OnSave(SettingsSaveMessage message)
            => _bridge.OnSave(message.Path);
    }
}
