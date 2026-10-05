using MyChart.Core.Plugins.Manifest;
using MyChart.Core.Plugins.Vocabulary;
using Xunit;

namespace MyChart.Tests.Plugins;

/// <summary>AT14 VocabularyVectors (subset without six first-party tools).</summary>
public class DefinitionValidatorTests
{
    private static string Def(string body) =>
        """{"schema":"mychart.tooldef","vocabularyVersion":"1.0",""" + body + "}";

    [Fact]
    public void Valid_MinimalLine()
    {
        var json = Def("""
            "anchors":2,"workflow":"Click","shapes":[{"kind":"Line","from":{"x":{"anchor":0},"y":{"anchor":0}},"to":{"x":{"anchor":1},"y":{"anchor":1}}}]
            """);
        var code = DefinitionValidator.ValidateJson(json, out var def);
        Assert.Null(code);
        Assert.NotNull(def);
        Assert.Equal(2, def!.Anchors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(9)]
    public void Anchors_OutOfRange_E019(int n)
    {
        var json = Def($"\"anchors\":{n},\"workflow\":\"Click\",\"shapes\":[]");
        Assert.Equal(ErrorCodes.DefinitionSchemaError, DefinitionValidator.ValidateJson(json, out _));
    }

    [Fact]
    public void UnknownProperty_E019()
    {
        var json = """{"schema":"mychart.tooldef","vocabularyVersion":"1.0","anchors":1,"colour":"red","shapes":[]}""";
        Assert.Equal(ErrorCodes.DefinitionSchemaError, DefinitionValidator.ValidateJson(json, out _));
    }

    [Fact]
    public void UnknownShape_E021()
    {
        var json = Def("\"anchors\":1,\"shapes\":[{\"kind\":\"Spiral\"}]");
        Assert.Equal(ErrorCodes.VocabularyUnsupported, DefinitionValidator.ValidateJson(json, out _));
    }

    [Theory]
    [InlineData("2.0")]
    [InlineData("1.1")]
    public void VocabularyVersion_Unsupported_E021(string v)
    {
        var json = $"{{\"schema\":\"mychart.tooldef\",\"vocabularyVersion\":\"{v}\",\"anchors\":1,\"shapes\":[]}}";
        Assert.Equal(ErrorCodes.VocabularyUnsupported, DefinitionValidator.ValidateJson(json, out _));
    }

    [Fact]
    public void SeventeenShapes_E022()
    {
        var shapes = string.Join(",", Enumerable.Range(0, 17).Select(_ => "{\"kind\":\"Line\",\"from\":{\"x\":{\"anchor\":0},\"y\":{\"anchor\":0}},\"to\":{\"x\":{\"anchor\":0},\"y\":{\"anchor\":0}}}"));
        var json = Def($"\"anchors\":1,\"shapes\":[{shapes}]");
        Assert.Equal(ErrorCodes.LimitExceeded, DefinitionValidator.ValidateJson(json, out _));
    }

    [Fact]
    public void SixtyFiveLevels_E022()
    {
        var levels = string.Join(",", Enumerable.Range(0, 65).Select(i => i.ToString()));
        var json = Def($"\"anchors\":2,\"shapes\":[{{\"kind\":\"LevelLines\",\"levels\":[{levels}],\"x1\":{{\"anchor\":0}},\"x2\":{{\"anchor\":1}},\"price\":\"level\"}}]");
        Assert.Equal(ErrorCodes.LimitExceeded, DefinitionValidator.ValidateJson(json, out _));
    }

    [Fact]
    public void ClickThenText_Anchors2_E019()
    {
        var json = Def("\"anchors\":2,\"workflow\":\"ClickThenText\",\"shapes\":[]");
        Assert.Equal(ErrorCodes.DefinitionSchemaError, DefinitionValidator.ValidateJson(json, out _));
    }

    [Fact]
    public void NamedForwardReference_E019()
    {
        var json = Def("""
            "anchors":1,"named":[{"name":"a","expr":"named.b"},{"name":"b","expr":"1"}],"shapes":[]
            """);
        Assert.Equal(ErrorCodes.DefinitionSchemaError, DefinitionValidator.ValidateJson(json, out _));
    }

    [Fact]
    public void PointAnchorOutOfRange_E019()
    {
        var json = Def("""
            "anchors":2,"shapes":[{"kind":"Line","from":{"x":{"anchor":2},"y":{"anchor":0}},"to":{"x":{"anchor":1},"y":{"anchor":1}}}]
            """);
        Assert.Equal(ErrorCodes.DefinitionSchemaError, DefinitionValidator.ValidateJson(json, out _));
    }
}
