using MyChart.Core.Serialization;

namespace MyChart.Core.UI.Workspace;

/// <summary>T6.09 layout presets (charts count and grid).</summary>
public enum WorkspaceLayoutKind
{
    Charts1 = 1,
    Charts2 = 2,
    Charts4 = 4,
    Charts6 = 6,
    Charts8 = 8
}

/// <summary>Grid size for a layout kind.</summary>
public readonly record struct WorkspaceGrid(int Rows, int Columns);

/// <summary>One chart cell: independent symbol, timeframe, view, drawings, indicators.</summary>
public sealed class WorkspaceChartCell
{
    public string CellId { get; set; } = Guid.NewGuid().ToString("N");
    public WorkspaceDocument Document { get; set; } = new();
}

/// <summary>
/// T6.09 Workspace model — multi-chart layout.
/// LAYOUTS: 1=1x1, 2=1x2, 4=2x2, 6=2x3, 8=2x4.
/// Each cell independent Symbol/Timeframe/view/drawings.
/// OUT_OF_SCOPE_V1: detached/floating/multi-monitor (no stubs).
/// </summary>
public sealed class WorkspaceModel
{
    public const string FileExtension = ".mychart.json";
    public const string AutosaveFileName = "_autosave.mychart.json";
    public const int AutosaveIntervalSeconds = 60;

    public WorkspaceLayoutKind Layout { get; private set; }
    public IReadOnlyList<WorkspaceChartCell> Cells => _cells;
    private readonly List<WorkspaceChartCell> _cells = new();

    public WorkspaceModel(WorkspaceLayoutKind layout = WorkspaceLayoutKind.Charts1)
    {
        SetLayout(layout);
    }

    public static WorkspaceGrid GridFor(WorkspaceLayoutKind layout) => layout switch
    {
        WorkspaceLayoutKind.Charts1 => new WorkspaceGrid(1, 1),
        WorkspaceLayoutKind.Charts2 => new WorkspaceGrid(1, 2),
        WorkspaceLayoutKind.Charts4 => new WorkspaceGrid(2, 2),
        WorkspaceLayoutKind.Charts6 => new WorkspaceGrid(2, 3),
        WorkspaceLayoutKind.Charts8 => new WorkspaceGrid(2, 4),
        _ => new WorkspaceGrid(1, 1)
    };

    public static int ChartCount(WorkspaceLayoutKind layout) => (int)layout;

    /// <summary>Change layout; keeps existing cells by index; pads with empty default charts.</summary>
    public void SetLayout(WorkspaceLayoutKind layout)
    {
        Layout = layout;
        var n = ChartCount(layout);
        while (_cells.Count < n)
            _cells.Add(CreateDefaultCell());
        while (_cells.Count > n)
            _cells.RemoveAt(_cells.Count - 1);
    }

    public static WorkspaceChartCell CreateDefaultCell(string symbol = "EURUSD")
    {
        var cell = new WorkspaceChartCell();
        cell.Document.View.Symbol = symbol;
        return cell;
    }

    /// <summary>Default workspace: one chart (startup when no autosave).</summary>
    public static WorkspaceModel CreateDefault() => new(WorkspaceLayoutKind.Charts1);
}

/// <summary>
/// T6.09 paths and file operations (Save/Load/Export/Import, autosave).
/// PATH: %AppData%/MyChart/Workspaces/
/// Pure path logic + JSON via WorkspaceDocument; caller supplies root directory for tests.
/// </summary>
public sealed class WorkspaceStore
{
    private readonly string _workspacesDir;

    public WorkspaceStore(string workspacesDirectory)
    {
        _workspacesDir = workspacesDirectory ?? throw new ArgumentNullException(nameof(workspacesDirectory));
    }

    public string WorkspacesDirectory => _workspacesDir;
    public string AutosavePath => Path.Combine(_workspacesDir, WorkspaceModel.AutosaveFileName);

    public static string DefaultWorkspacesDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "MyChart", "Workspaces");
    }

    public void EnsureDirectory()
    {
        Directory.CreateDirectory(_workspacesDir);
    }

    public void Save(WorkspaceModel workspace, string filePath)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        EnsureDirectory();
        var json = Serialize(workspace);
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(filePath, json);
    }

    public WorkspaceModel Load(string filePath)
    {
        var json = File.ReadAllText(filePath);
        return Deserialize(json);
    }

    public void SaveAutosave(WorkspaceModel workspace) => Save(workspace, AutosavePath);

    public WorkspaceModel? TryLoadAutosave()
    {
        if (!File.Exists(AutosavePath))
            return null;
        return Load(AutosavePath);
    }

    /// <summary>Startup: autosave if present, otherwise one default chart.</summary>
    public WorkspaceModel LoadOnStartup()
    {
        return TryLoadAutosave() ?? WorkspaceModel.CreateDefault();
    }

    public void Export(WorkspaceModel workspace, string exportPath) => Save(workspace, exportPath);

    public WorkspaceModel Import(string importPath) => Load(importPath);

    public static string Serialize(WorkspaceModel workspace)
    {
        var cells = new System.Text.Json.Nodes.JsonArray();
        foreach (var cell in workspace.Cells)
        {
            var cellDoc = System.Text.Json.Nodes.JsonNode.Parse(cell.Document.Save())
                          ?? new System.Text.Json.Nodes.JsonObject();
            cells.Add(new System.Text.Json.Nodes.JsonObject
            {
                ["cellId"] = cell.CellId,
                ["document"] = cellDoc
            });
        }

        var root = new System.Text.Json.Nodes.JsonObject
        {
            ["schema"] = "mychart.workspace.layout",
            ["version"] = 1,
            ["layout"] = workspace.Layout.ToString(),
            ["chartCount"] = WorkspaceModel.ChartCount(workspace.Layout),
            ["cells"] = cells
        };
        return root.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }

    public static WorkspaceModel Deserialize(string json, Func<string, int, bool>? isKnownDrawingType = null)
    {
        var root = System.Text.Json.Nodes.JsonNode.Parse(json) as System.Text.Json.Nodes.JsonObject
                   ?? throw new InvalidOperationException("Invalid workspace JSON.");

        var layoutName = root["layout"]?.GetValue<string>() ?? nameof(WorkspaceLayoutKind.Charts1);
        if (!Enum.TryParse<WorkspaceLayoutKind>(layoutName, true, out var layout))
            layout = WorkspaceLayoutKind.Charts1;

        var model = new WorkspaceModel(layout);
        var cellsNode = root["cells"] as System.Text.Json.Nodes.JsonArray;
        if (cellsNode is null)
            return model;

        var known = isKnownDrawingType ?? ((_, _) => true);
        var list = new List<WorkspaceChartCell>();
        foreach (var node in cellsNode)
        {
            if (node is not System.Text.Json.Nodes.JsonObject co)
                continue;
            var cell = new WorkspaceChartCell
            {
                CellId = co["cellId"]?.GetValue<string>() ?? Guid.NewGuid().ToString("N")
            };
            if (co["document"] is System.Text.Json.Nodes.JsonNode docNode)
                cell.Document = WorkspaceDocument.Load(docNode.ToJsonString(), known);
            list.Add(cell);
        }

        // Apply loaded cells respecting layout count
        model.SetLayout(layout);
        for (var i = 0; i < model.Cells.Count && i < list.Count; i++)
        {
            model.Cells[i].CellId = list[i].CellId;
            model.Cells[i].Document = list[i].Document;
        }
        return model;
    }
}
