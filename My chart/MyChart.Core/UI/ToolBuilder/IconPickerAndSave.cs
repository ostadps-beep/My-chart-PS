using MyChart.Core.Plugins.Identity;
using MyChart.Core.Plugins.Vocabulary;
using MyChart.Core.UI.ToolBuilder;

namespace MyChart.Core.UI.ToolBuilder;

/// <summary>One icon entry for the picker (catalog or UserRoot).</summary>
public sealed record IconListItem(string ComponentId, string IconKey, string Source);

/// <summary>
/// PG6.03 IconPicker — lists catalog + UserRoot icons; paste SVG path validated by GeometryPathGrammar.
/// </summary>
public sealed class IconPickerModel
{
    private readonly List<IconListItem> _items = new();

    public IReadOnlyList<IconListItem> Items => _items;
    public string? SelectedIconKey { get; set; }
    public string? GeometryPathData { get; set; }
    public string? GeometryError { get; private set; }

    public void LoadFromDirectories(params string[] roots)
    {
        _items.Clear();
        foreach (var root in roots)
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                continue;
            var source = root;
            foreach (var dir in Directory.EnumerateDirectories(root))
            {
                var id = Path.GetFileName(dir);
                if (id is null) continue;
                if (!id.StartsWith("Icon", StringComparison.OrdinalIgnoreCase) &&
                    !File.Exists(Path.Combine(dir, "Geometry.txt")))
                    continue;
                var key = id.StartsWith("Icon", StringComparison.Ordinal)
                    ? "Icon." + id["Icon".Length..]
                    : "Icon." + id;
                _items.Add(new IconListItem(id, key, source));
            }
        }
        _items.Sort((a, b) => string.CompareOrdinal(a.IconKey, b.IconKey));
    }

    public bool ValidateGeometry(string? pathData)
    {
        GeometryPathData = pathData;
        if (!GeometryPathGrammar.IsValid(pathData))
        {
            GeometryError = "Invalid geometry path (GeometryPathGrammar).";
            return false;
        }
        GeometryError = null;
        return true;
    }
}

/// <summary>Result of a save attempt to UserRoot.</summary>
public sealed record ToolSaveResult(
    bool Ok,
    string? FolderPath,
    string? ErrorCode,
    string? Message,
    bool RestartRequired);

/// <summary>
/// PG6.03 Save pipeline — writes to UserRoot; keeps Definition.json.bak;
/// blocks when conformance C1/C2/C9 fail. Tool appears at next host start.
/// </summary>
public sealed class ToolSavePipeline
{
    private readonly string _userRoot;

    public ToolSavePipeline(string userRoot)
    {
        _userRoot = userRoot ?? throw new ArgumentNullException(nameof(userRoot));
    }

    public string UserRoot => _userRoot;

    /// <summary>
    /// C1 = definition validates; C2 = ComponentId valid; C9 = folder writable / not conflicting badly.
    /// </summary>
    public ToolSaveResult Save(ToolDefinitionFormModel form, string? iconKey = null)
    {
        // C2 — ComponentId rules (letters/digits, length)
        var id = form.Name.Trim();
        var idErr = IdRules.Validate(id);
        if (idErr is not null)
            return new ToolSaveResult(false, null, idErr, "C2: invalid ComponentId", false);

        // C1 — definition must validate
        var json = form.TryGetSaveableDefinition();
        if (json is null)
            return new ToolSaveResult(false, null, "C1", form.ValidationError ?? "C1: definition invalid", false);

        var err = DefinitionValidator.ValidateJson(json, out _);
        if (err is not null)
            return new ToolSaveResult(false, null, "C1", err, false);

        // C9 — must be able to create folder under UserRoot
        string folder;
        try
        {
            Directory.CreateDirectory(_userRoot);
            folder = Path.Combine(_userRoot, id);
        }
        catch (Exception ex)
        {
            return new ToolSaveResult(false, null, "C9", "C9: " + ex.Message, false);
        }

        var defPath = Path.Combine(folder, "Definition.json");
        try
        {
            if (Directory.Exists(folder) && File.Exists(defPath))
            {
                var bak = defPath + ".bak";
                File.Copy(defPath, bak, overwrite: true);
            }
            Directory.CreateDirectory(folder);
            File.WriteAllText(defPath, json);

            // Minimal Manifest so UserComponentLoader can see the folder later
            var manifest = "{\n  \"schema\": \"mychart.manifest\",\n  \"componentId\": \"" + id +
                           "\",\n  \"kind\": \"DataTool\",\n  \"name\": \"" + id + "\"" +
                           (iconKey is null ? "" : ",\n  \"iconKey\": \"" + iconKey + "\"") +
                           "\n}\n";
            File.WriteAllText(Path.Combine(folder, "Manifest.json"), manifest);
        }
        catch (Exception ex)
        {
            return new ToolSaveResult(false, folder, "C9", "C9: write failed: " + ex.Message, false);
        }

        return new ToolSaveResult(
            Ok: true,
            FolderPath: folder,
            ErrorCode: null,
            Message: "Saved. Tool appears at the next application start. Restart recommended.",
            RestartRequired: true);
    }
}
