namespace ChartMy.Services;


public sealed record SettingsUpdateMessage(string Key, object? Value);

public sealed record SettingsAppliedMessage(string Status, IReadOnlyList<string> Changed);

public sealed record SettingsSaveMessage(string Path);

public interface ISettingsProtocolListener
{
    void OnUpdate(SettingsUpdateMessage message);
    void OnApplied(SettingsAppliedMessage message);
    void OnSave(SettingsSaveMessage message);
}

public sealed class SettingsProtocol
{
    private readonly List<ISettingsProtocolListener> _listeners = [];

    public IReadOnlyList<string> History { get; private set; } = [];

    public void Subscribe(ISettingsProtocolListener listener) => _listeners.Add(listener);

    public void PublishUpdate(string key, object? value)
    {
        var message = new SettingsUpdateMessage(key, value);
        Remember($"SETTINGS.UPDATE {{ key: \"{key}\", value: {Format(value)} }}");
        foreach (var listener in _listeners)
            listener.OnUpdate(message);
    }

    public void PublishApplied(IReadOnlyList<string> changed, string status = "success")
    {
        var message = new SettingsAppliedMessage(status, changed);
        var list = string.Join(", ", changed.Select(k => $"\"{k}\""));
        Remember($"SETTINGS.APPLIED {{ status: \"{status}\", changed: [{list}] }}");
        foreach (var listener in _listeners)
            listener.OnApplied(message);
    }

    public void PublishSave(string path)
    {
        var message = new SettingsSaveMessage(path);
        Remember($"SETTINGS.SAVE {{ path: \"{path}\" }}");
        foreach (var listener in _listeners)
            listener.OnSave(message);
    }

    private void Remember(string line)
    {
        var next = History.ToList();
        next.Add(line);
        if (next.Count > 200)
            next.RemoveAt(0);
        History = next;
        System.Diagnostics.Debug.WriteLine(line);
    }

    private static string Format(object? value) =>
        value switch
        {
            null => "null",
            string s => $"\"{s}\"",
            bool b => b ? "true" : "false",
            _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? ""
        };
}
