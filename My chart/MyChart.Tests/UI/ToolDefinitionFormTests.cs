using MyChart.Core.Plugins.Vocabulary;
using MyChart.Core.UI.ToolBuilder;
using Xunit;

namespace MyChart.Tests.UI;

/// <summary>PG6.01 BuilderShellAndForm — view-model/form validation (GATE=VISUAL pending).</summary>
public class ToolDefinitionFormTests
{
    [Fact]
    public void Empty_Name_Blocks_Save()
    {
        var m = new ToolDefinitionFormModel { Name = "", Anchors = 2 };
        m.AddShape("Line");
        Assert.Null(m.TryGetSaveableDefinition());
        Assert.False(string.IsNullOrEmpty(m.ValidationError));
    }

    [Fact]
    public void Invalid_Anchors_Blocked()
    {
        var m = new ToolDefinitionFormModel { Name = "X", Anchors = 0 };
        Assert.Null(m.TryGetSaveableDefinition());
    }

    [Fact]
    public void ClickThenText_Requires_One_Anchor()
    {
        var m = new ToolDefinitionFormModel
        {
            Name = "Label",
            Anchors = 2,
            Workflow = "ClickThenText"
        };
        m.AddShape("Line");
        Assert.Null(m.TryGetSaveableDefinition());
        Assert.Contains("ClickThenText", m.ValidationError);
    }

    [Fact]
    public void Valid_TrendLine_Template_Saves()
    {
        var m = ToolDefinitionFormModel.FromTrendLineTemplate();
        var json = m.TryGetSaveableDefinition();
        Assert.NotNull(json);
        var err = DefinitionValidator.ValidateJson(json!, out var def);
        Assert.Null(err);
        Assert.NotNull(def);
        Assert.Equal(2, def!.Anchors);
        Assert.Equal("Click", def.Workflow);
    }

    [Fact]
    public void Shape_Add_Remove_Reorder()
    {
        var m = new ToolDefinitionFormModel { Name = "T", Anchors = 2 };
        m.AddShape("Line");
        m.AddShape("Rect");
        Assert.Equal(2, m.Shapes.Count);
        Assert.True(m.MoveShape(0, 1));
        Assert.Equal("Rect", m.Shapes[0].Kind);
        Assert.True(m.RemoveShape(0));
        Assert.Single(m.Shapes);
        Assert.Equal("Line", m.Shapes[0].Kind);
    }

    [Fact]
    public void Invalid_Never_Returned_As_Saveable()
    {
        var m = new ToolDefinitionFormModel { Name = "Bad", Anchors = 9 };
        Assert.False(m.Validate());
        Assert.Null(m.TryGetSaveableDefinition());
    }

    [Fact]
    public void BuildDefinitionJson_Has_Schema_And_Vocabulary()
    {
        var m = ToolDefinitionFormModel.FromTrendLineTemplate();
        var json = m.BuildDefinitionJson();
        Assert.Contains("mychart.tooldef", json);
        Assert.Contains(VocabularyVersions.Current, json);
        Assert.Contains("Line", json);
    }
}
