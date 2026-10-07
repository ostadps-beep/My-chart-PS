using MyChart.Core.Models.Settings;

namespace MyChart.App.Integration;

/// <summary>
/// T6.08 App host — only App file that will reference MyChart.Settings panel (R2).
/// Forwards protocol messages to Core SettingsBridge. Real SettingsWindow attach is owner step
/// when MyChart.Settings project is available on the machine.
/// </summary>
public sealed class SettingsBridgeHost : ISettingsProtocolListener
{
    private readonly SettingsBridge _bridge;

    public SettingsBridgeHost(SettingsBridge bridge) => _bridge = bridge;

    public SettingsBridge Bridge => _bridge;

    public void OnUpdate(SettingsUpdateMessage message)
        => _bridge.OnUpdate(message.Key, message.Value);

    public void OnApplied(SettingsAppliedMessage message)
        => _bridge.OnApplied(message.Changed);

    public void OnSave(SettingsSaveMessage message)
        => _bridge.OnSave(message.Path);

    /// <summary>R9 — opens Settings window when MyChart.Settings is linked; otherwise no-op.</summary>
    public void ShowSettingsWindow(System.Windows.Window? owner)
    {
        // Placeholder: when MyChart.Settings is added to the solution, resolve SettingsWindow here.
        // Owner attaches panel per SETTINGS_INTEGRATION R1 mechanical rename.
        System.Diagnostics.Debug.WriteLine("[Settings] ShowSettingsWindow — panel project not linked yet.");
    }
}
