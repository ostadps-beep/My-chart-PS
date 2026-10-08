using MyChart.Core.Serialization;
using MyChart.Core.UI.Workspace;
using Xunit;

namespace MyChart.Tests.UI;

/// <summary>T6.09 Workspace — layouts, per-cell independence, save/load/autosave (GATE=VISUAL pending).</summary>
public class WorkspaceTests : IDisposable
{
    private readonly string _dir;

    public WorkspaceTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "MyChartWorkspaceTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); } catch { /* ignore */ }
    }

    [Theory]
    [InlineData(WorkspaceLayoutKind.Charts1, 1, 1, 1)]
    [InlineData(WorkspaceLayoutKind.Charts2, 2, 1, 2)]
    [InlineData(WorkspaceLayoutKind.Charts4, 4, 2, 2)]
    [InlineData(WorkspaceLayoutKind.Charts6, 6, 2, 3)]
    [InlineData(WorkspaceLayoutKind.Charts8, 8, 2, 4)]
    public void Layout_Grid_Matches_Spec(WorkspaceLayoutKind kind, int count, int rows, int cols)
    {
        Assert.Equal(count, WorkspaceModel.ChartCount(kind));
        var g = WorkspaceModel.GridFor(kind);
        Assert.Equal(rows, g.Rows);
        Assert.Equal(cols, g.Columns);
        var ws = new WorkspaceModel(kind);
        Assert.Equal(count, ws.Cells.Count);
    }

    [Fact]
    public void Default_Is_One_Chart()
    {
        var ws = WorkspaceModel.CreateDefault();
        Assert.Equal(WorkspaceLayoutKind.Charts1, ws.Layout);
        Assert.Single(ws.Cells);
        Assert.Equal("EURUSD", ws.Cells[0].Document.View.Symbol);
    }

    [Fact]
    public void Cells_Are_Independent()
    {
        var ws = new WorkspaceModel(WorkspaceLayoutKind.Charts2);
        ws.Cells[0].Document.View.Symbol = "EURUSD";
        ws.Cells[1].Document.View.Symbol = "GBPUSD";
        Assert.Equal("EURUSD", ws.Cells[0].Document.View.Symbol);
        Assert.Equal("GBPUSD", ws.Cells[1].Document.View.Symbol);
    }

    [Fact]
    public void SetLayout_Pads_And_Trims()
    {
        var ws = new WorkspaceModel(WorkspaceLayoutKind.Charts4);
        ws.Cells[0].Document.View.Symbol = "XAUUSD";
        ws.SetLayout(WorkspaceLayoutKind.Charts1);
        Assert.Single(ws.Cells);
        Assert.Equal("XAUUSD", ws.Cells[0].Document.View.Symbol);
        ws.SetLayout(WorkspaceLayoutKind.Charts4);
        Assert.Equal(4, ws.Cells.Count);
        Assert.Equal("XAUUSD", ws.Cells[0].Document.View.Symbol);
    }

    [Fact]
    public void Autosave_Interval_Is_60_Seconds()
    {
        Assert.Equal(60, WorkspaceModel.AutosaveIntervalSeconds);
        Assert.Equal("_autosave.mychart.json", WorkspaceModel.AutosaveFileName);
        Assert.Equal(".mychart.json", WorkspaceModel.FileExtension);
    }

    [Fact]
    public void Save_Load_RoundTrip()
    {
        var store = new WorkspaceStore(_dir);
        var ws = new WorkspaceModel(WorkspaceLayoutKind.Charts2);
        ws.Cells[0].Document.View.Symbol = "EURUSD";
        ws.Cells[1].Document.View.Symbol = "USDJPY";

        var path = Path.Combine(_dir, "desk1" + WorkspaceModel.FileExtension);
        store.Save(ws, path);
        var loaded = store.Load(path);

        Assert.Equal(WorkspaceLayoutKind.Charts2, loaded.Layout);
        Assert.Equal(2, loaded.Cells.Count);
        Assert.Equal("EURUSD", loaded.Cells[0].Document.View.Symbol);
        Assert.Equal("USDJPY", loaded.Cells[1].Document.View.Symbol);
    }

    [Fact]
    public void Autosave_And_Startup()
    {
        var store = new WorkspaceStore(_dir);
        Assert.Null(store.TryLoadAutosave());
        Assert.Equal(WorkspaceLayoutKind.Charts1, store.LoadOnStartup().Layout);

        var ws = new WorkspaceModel(WorkspaceLayoutKind.Charts4);
        ws.Cells[2].Document.View.Symbol = "BTCUSD";
        store.SaveAutosave(ws);

        Assert.True(File.Exists(store.AutosavePath));
        var started = store.LoadOnStartup();
        Assert.Equal(WorkspaceLayoutKind.Charts4, started.Layout);
        Assert.Equal("BTCUSD", started.Cells[2].Document.View.Symbol);
    }

    [Fact]
    public void Export_Import()
    {
        var store = new WorkspaceStore(_dir);
        var ws = new WorkspaceModel(WorkspaceLayoutKind.Charts1);
        ws.Cells[0].Document.View.Symbol = "AUDUSD";
        var exportPath = Path.Combine(_dir, "export" + WorkspaceModel.FileExtension);
        store.Export(ws, exportPath);
        var imported = store.Import(exportPath);
        Assert.Equal("AUDUSD", imported.Cells[0].Document.View.Symbol);
    }

    [Fact]
    public void Serialize_Deserialize_ByteStable_On_Second_Save()
    {
        var ws = new WorkspaceModel(WorkspaceLayoutKind.Charts2);
        ws.Cells[0].Document.View.Symbol = "EURUSD";
        ws.Cells[1].Document.View.Symbol = "GBPUSD";
        var json1 = WorkspaceStore.Serialize(ws);
        var loaded = WorkspaceStore.Deserialize(json1);
        var json2 = WorkspaceStore.Serialize(loaded);
        var loaded2 = WorkspaceStore.Deserialize(json2);
        var json3 = WorkspaceStore.Serialize(loaded2);
        Assert.Equal(json2, json3);
    }
}
