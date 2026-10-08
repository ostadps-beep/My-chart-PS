using MyChart.Core.Plugins.Identity;
using MyChart.Core.UI.ToolBuilder;
using Xunit;

namespace MyChart.Tests.UI;

/// <summary>PG6.03 IconPickerAndSave (GATE=VISUAL pending).</summary>
public class IconPickerSaveTests : IDisposable
{
    private readonly string _userRoot;
    private readonly string _catalog;

    public IconPickerSaveTests()
    {
        _userRoot = Path.Combine(Path.GetTempPath(), "MyChartUser_" + Guid.NewGuid().ToString("N"));
        _catalog = Path.Combine(Path.GetTempPath(), "MyChartCat_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_userRoot);
        Directory.CreateDirectory(Path.Combine(_catalog, "IconCursor"));
        File.WriteAllText(Path.Combine(_catalog, "IconCursor", "Geometry.txt"), "M0 0 L16 16");
    }

    public void Dispose()
    {
        try { Directory.Delete(_userRoot, true); } catch { }
        try { Directory.Delete(_catalog, true); } catch { }
    }

    [Fact]
    public void IconPicker_Lists_Catalog_Icons()
    {
        var picker = new IconPickerModel();
        picker.LoadFromDirectories(_catalog, _userRoot);
        Assert.Contains(picker.Items, i => i.IconKey.Contains("Cursor", StringComparison.Ordinal));
    }

    [Fact]
    public void Geometry_Paste_Validated()
    {
        var picker = new IconPickerModel();
        Assert.False(picker.ValidateGeometry("not-a-path"));
        Assert.NotNull(picker.GeometryError);
        Assert.True(picker.ValidateGeometry("M0 0 L10 10"));
        Assert.Null(picker.GeometryError);
    }

    [Fact]
    public void Save_Valid_Creates_Folder_In_UserRoot()
    {
        var pipe = new ToolSavePipeline(_userRoot);
        var form = ToolDefinitionFormModel.FromTrendLineTemplate();
        form.Name = "MyLine";
        var result = pipe.Save(form, iconKey: "Icon.Cursor");
        Assert.True(result.Ok);
        Assert.True(Directory.Exists(Path.Combine(_userRoot, "MyLine")));
        Assert.True(File.Exists(Path.Combine(_userRoot, "MyLine", "Definition.json")));
        Assert.True(File.Exists(Path.Combine(_userRoot, "MyLine", "Manifest.json")));
        Assert.True(result.RestartRequired);
    }

    [Fact]
    public void Save_Keeps_Bak_On_Update()
    {
        var pipe = new ToolSavePipeline(_userRoot);
        var form = ToolDefinitionFormModel.FromTrendLineTemplate();
        form.Name = "MyLine";
        Assert.True(pipe.Save(form).Ok);
        form.Anchors = 2;
        Assert.True(pipe.Save(form).Ok);
        Assert.True(File.Exists(Path.Combine(_userRoot, "MyLine", "Definition.json.bak")));
    }

    [Fact]
    public void Save_Blocked_When_C1_Invalid()
    {
        var pipe = new ToolSavePipeline(_userRoot);
        var form = new ToolDefinitionFormModel { Name = "X", Anchors = 0 };
        var result = pipe.Save(form);
        Assert.False(result.Ok);
        Assert.Equal("C1", result.ErrorCode);
        Assert.False(Directory.Exists(Path.Combine(_userRoot, "X")));
    }

    [Fact]
    public void Save_Blocked_When_C2_Bad_Id()
    {
        var pipe = new ToolSavePipeline(_userRoot);
        var form = ToolDefinitionFormModel.FromTrendLineTemplate();
        form.Name = "bad id!"; // invalid id chars
        var result = pipe.Save(form);
        Assert.False(result.Ok);
        // C2 or id error code
        Assert.False(string.IsNullOrEmpty(result.ErrorCode));
        Assert.DoesNotContain(Directory.GetDirectories(_userRoot), d => Path.GetFileName(d) == "bad id!");
    }

    [Fact]
    public void UserRoot_Folder_Visible_To_Loader_Scan()
    {
        var pipe = new ToolSavePipeline(_userRoot);
        var form = ToolDefinitionFormModel.FromTrendLineTemplate();
        form.Name = "ScanMe";
        Assert.True(pipe.Save(form).Ok);
        var dirs = Directory.GetDirectories(_userRoot);
        Assert.Contains(dirs, d => Path.GetFileName(d) == "ScanMe");
        Assert.True(File.Exists(Path.Combine(_userRoot, "ScanMe", "Manifest.json")));
    }
}
