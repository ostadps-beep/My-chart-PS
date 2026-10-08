using MyChart.Core.UI.ToolBuilder;
using Xunit;

namespace MyChart.Tests.UI;

/// <summary>PG6.02 LivePreview — debounce 250ms, keep last valid, states, anchors (GATE=VISUAL pending).</summary>
public class LivePreviewTests
{
    [Fact]
    public void Debounce_Is_250_Ms()
    {
        Assert.Equal(250, LivePreviewEngine.DebounceMilliseconds);
    }

    [Fact]
    public void Preview_Host_Is_Separate()
    {
        Assert.Equal("toolbuilder.preview.host", LivePreviewEngine.PreviewHostId);
    }

    [Fact]
    public void Valid_Change_Applies_After_Debounce()
    {
        var engine = new LivePreviewEngine();
        var form = ToolDefinitionFormModel.FromTrendLineTemplate();
        engine.OnFormChanged(form, nowMs: 0);
        Assert.True(engine.HasPendingDebounce);
        Assert.Null(engine.LastValid);

        Assert.False(engine.Tick(100));
        Assert.Null(engine.LastValid);

        Assert.True(engine.Tick(250));
        Assert.NotNull(engine.LastValid);
        Assert.Contains("TrendLine", form.Name);
        Assert.Contains("Line", engine.LastValid!.DefinitionJson);
    }

    [Fact]
    public void Invalid_Keeps_Last_Valid_Preview()
    {
        var engine = new LivePreviewEngine();
        var good = ToolDefinitionFormModel.FromTrendLineTemplate();
        Assert.True(engine.ApplyNow(good));
        var version = engine.LastValid!.Version;
        var json = engine.LastValid.DefinitionJson;

        var bad = new ToolDefinitionFormModel { Name = "Bad", Anchors = 0 };
        engine.OnFormChanged(bad, nowMs: 1000);
        Assert.False(engine.HasPendingDebounce);
        Assert.Equal(version, engine.LastValid!.Version);
        Assert.Equal(json, engine.LastValid.DefinitionJson);
    }

    [Fact]
    public void Sample_Anchors_Match_Form_Anchors_Count()
    {
        var engine = new LivePreviewEngine();
        var form = ToolDefinitionFormModel.FromTrendLineTemplate();
        engine.ApplyNow(form);
        Assert.Equal(2, engine.Anchors.Count);
    }

    [Fact]
    public void Drag_Anchor_Updates_Position()
    {
        var engine = new LivePreviewEngine();
        engine.SetSampleAnchors(2);
        Assert.True(engine.DragAnchor(1, 400, 200));
        Assert.Equal(400, engine.Anchors[1].XDip);
        Assert.Equal(200, engine.Anchors[1].YDip);
    }

    [Fact]
    public void States_Normal_Selected_Preview()
    {
        var engine = new LivePreviewEngine();
        engine.SetState(LivePreviewState.Normal);
        Assert.Equal(LivePreviewState.Normal, engine.State);
        engine.SetState(LivePreviewState.Selected);
        Assert.Equal(LivePreviewState.Selected, engine.State);
        engine.SetState(LivePreviewState.Preview);
        Assert.Equal(LivePreviewState.Preview, engine.State);
    }

    [Fact]
    public void Per_Shape_Expression_Error_Recorded()
    {
        var engine = new LivePreviewEngine();
        var form = ToolDefinitionFormModel.FromTrendLineTemplate();
        form.Shapes[0].ExpressionError = "E020: bad expr";
        // still valid definition structurally
        engine.OnFormChanged(form, 0);
        Assert.True(engine.ShapeErrors.ContainsKey(0));
        Assert.Contains("E020", engine.ShapeErrors[0]);
    }

    [Fact]
    public void Rapid_Changes_Only_Last_Pending_Wins()
    {
        var engine = new LivePreviewEngine();
        var form = ToolDefinitionFormModel.FromTrendLineTemplate();
        engine.OnFormChanged(form, 0);
        form.Name = "TrendLine2";
        engine.OnFormChanged(form, 50);
        Assert.True(engine.Tick(300));
        Assert.Contains("anchors", engine.LastValid!.DefinitionJson);
    }
}
