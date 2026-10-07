using System.IO;
using MyChart.Settings.Model;

namespace MyChart.Settings.Services;

public sealed class SettingsController
{
    public const string RelativeSavePath = "settings/chart.json";
    public const string RelativeBackupPath = "settings/chart.backup.json";

    private readonly SettingsProtocol _protocol;
    private SettingsDocument _working;
    private SettingsDocument _saved;
    private readonly HashSet<string> _pendingHeavy = new(StringComparer.Ordinal);

    public SettingsController(SettingsProtocol protocol, SettingsDocument? loaded = null)
    {
        _protocol = protocol;
        _working = loaded ?? LoadOrDefaults();
        _saved = _working.Clone();
    }

    public SettingsProtocol Protocol => _protocol;
    public SettingsDocument Working => _working;
    public string SavePath => Path.Combine(AppContext.BaseDirectory, RelativeSavePath);
    public string BackupPath => Path.Combine(AppContext.BaseDirectory, RelativeBackupPath);

    public event Action<IReadOnlyList<string>>? Changed;

    public bool IsDirty => !_working.EqualsValues(_saved);
    public bool HasPendingHeavy => _pendingHeavy.Count > 0;

    public object? Get(string key) => _working.Get(key);

    public void Set(string key, object? value)
    {
        if (!SettingsSchema.FieldsByKey.TryGetValue(key, out var field))
            return;
        if (field.Control == ControlKind.Command)
            return;

        _working.Set(key, value);
        var changed = new List<string> { key };

        if (field.ApplyMode == ApplyMode.Immediate)
        {
            _protocol.PublishUpdate(key, _working.Get(key));
            _protocol.PublishApplied(changed);
        }
        else
        {
            _pendingHeavy.Add(key);
        }

        Notify(changed);
    }

    public void Apply()
    {
        CommitPendingToProtocol();
        Persist(_working, SavePath);
        _saved = _working.Clone();
        Notify([]);
    }

    public void Ok(ISettingsUi ui)
    {
        Apply();
        ui.CloseWindow();
    }

    public void DiscardChanges() => RevertToSaved();

    public void Cancel(ISettingsUi ui)
    {
        DiscardChanges();
        ui.CloseWindow();
    }

    public void ResetCategory(string categoryId)
    {
        var changed = new List<string>();
        foreach (var field in SettingsSchema.FieldsInCategory(categoryId))
        {
            if (field.Control == ControlKind.Command)
                continue;
            _working.Set(field.Key, field.DefaultValue);
            changed.Add(field.Key);
            if (field.ApplyMode == ApplyMode.Immediate)
            {
                _protocol.PublishUpdate(field.Key, _working.Get(field.Key));
            }
            else
            {
                _pendingHeavy.Add(field.Key);
            }
        }

        if (changed.Count > 0)
            _protocol.PublishApplied(changed);
        Notify(changed);
    }

    public void ResetLayout()
    {
        var previous = _working.Clone();
        _working = SettingsDocument.FromDefaults();
        var changed = _working.DiffKeys(previous);
        foreach (var key in changed)
        {
            if (!SettingsSchema.FieldsByKey.TryGetValue(key, out var field))
                continue;
            if (field.ApplyMode == ApplyMode.Immediate)
                _protocol.PublishUpdate(key, _working.Get(key));
            else
                _pendingHeavy.Add(key);
        }

        if (changed.Count > 0)
            _protocol.PublishApplied(changed);
        Notify(changed);
    }

    public void SaveLayout()
    {
        Persist(_working, SavePath);
        _saved = _working.Clone();
        _pendingHeavy.Clear();
        Notify([]);
    }

    public bool LoadFromPath(string path)
    {
        if (!File.Exists(path))
            return false;

        var loaded = SettingsJson.FromJson(File.ReadAllText(path));
        ReplaceWorking(loaded);
        return true;
    }

    public void ExportTo(string path) => Persist(_working, path);

    public bool ImportFrom(string path) => LoadFromPath(path);

    public void Backup()
    {
        Persist(_working, BackupPath);
    }

    public bool RestoreBackup()
    {
        if (!File.Exists(BackupPath))
            return false;
        return LoadFromPath(BackupPath);
    }

    public bool IsEnabled(string key) =>
        SettingsSchema.FieldsByKey.TryGetValue(key, out var field)
        && DependencyEvaluator.IsEnabled(field, _working);

    private void ReplaceWorking(SettingsDocument loaded)
    {
        var previous = _working.Clone();
        _working = loaded;
        var changed = _working.DiffKeys(previous);
        foreach (var key in changed)
        {
            if (!SettingsSchema.FieldsByKey.TryGetValue(key, out var field))
                continue;
            if (field.ApplyMode == ApplyMode.Immediate)
                _protocol.PublishUpdate(key, _working.Get(key));
            else
                _pendingHeavy.Add(key);
        }

        if (changed.Count > 0)
            _protocol.PublishApplied(changed);
        Notify(changed);
    }

    private void RevertToSaved()
    {
        var current = _working.Clone();
        var diffs = _saved.DiffKeys(current);
        _working = _saved.Clone();
        _pendingHeavy.Clear();

        var revertedImmediate = new List<string>();
        foreach (var key in diffs)
        {
            if (!SettingsSchema.FieldsByKey.TryGetValue(key, out var field))
                continue;
            if (field.ApplyMode != ApplyMode.Immediate)
                continue;
            _protocol.PublishUpdate(key, _working.Get(key));
            revertedImmediate.Add(key);
        }

        if (revertedImmediate.Count > 0)
            _protocol.PublishApplied(revertedImmediate);
        Notify(diffs);
    }

    private void CommitPendingToProtocol()
    {
        if (_pendingHeavy.Count == 0)
            return;

        var changed = _pendingHeavy.ToList();
        foreach (var key in changed)
            _protocol.PublishUpdate(key, _working.Get(key));
        _protocol.PublishApplied(changed);
        _pendingHeavy.Clear();
    }

    private void Persist(SettingsDocument document, string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        File.WriteAllText(path, SettingsJson.ToJson(document));
        _protocol.PublishSave(ToRelative(path));
    }

    private static string ToRelative(string path)
    {
        var root = AppContext.BaseDirectory;
        return path.StartsWith(root, StringComparison.OrdinalIgnoreCase)
            ? path[root.Length..].Replace('\\', '/')
            : path.Replace('\\', '/');
    }

    private SettingsDocument LoadOrDefaults()
    {
        try
        {
            if (File.Exists(SavePath))
                return SettingsJson.FromJson(File.ReadAllText(SavePath));
        }
        catch
        {
            // Fall back to schema defaultValue.
        }

        return SettingsDocument.FromDefaults();
    }

    private void Notify(IReadOnlyList<string> keys) => Changed?.Invoke(keys);
}

