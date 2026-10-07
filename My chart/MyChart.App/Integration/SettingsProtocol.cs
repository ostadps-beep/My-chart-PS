namespace MyChart.App.Integration;

/// <summary>
/// T6.08 protocol messages (mirror of panel SETTINGS_INTEGRATION protocol).
/// Defined in App so Core never references the Settings panel types (R2).
/// </summary>
public readonly record struct SettingsUpdateMessage(string Key, object? Value);

public readonly record struct SettingsAppliedMessage(string Status, IReadOnlyList<string> Changed);

public readonly record struct SettingsSaveMessage(string Path);

/// <summary>Listener implemented by SettingsBridge (R5).</summary>
public interface ISettingsProtocolListener
{
    void OnUpdate(SettingsUpdateMessage message);
    void OnApplied(SettingsAppliedMessage message);
    void OnSave(SettingsSaveMessage message);
}
