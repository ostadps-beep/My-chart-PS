using System.Text;
using MyChart.Core.Analysis;
using MyChart.Core.Commands;
using MyChart.Core.Models.Drawing;
using MyChart.Core.Models.Market;
using MyChart.Core.Models.Rendering;
using MyChart.Core.Serialization;
using Xunit;

namespace MyChart.Tests.Serialization;

/// <summary>
/// T3.07 VERIFY: round trip of a workspace with 3 drawings and 1 indicator is byte-stable on the second save.
/// </summary>
public class WorkspaceSerializationTests
{
    private static readonly RgbaColor Accent = RgbaColor.FromRgb(33, 150, 243);

    private static DrawingObject Drawing(string id, string typeId = "test.segment")
    {
        return new DrawingObject(
            id,
            typeId,
            1,
            new[]
            {
                new DrawingAnchor(DateTimeOffset.Parse("2024-01-01T12:00:00Z"), 1.1000),
                new DrawingAnchor(DateTimeOffset.Parse("2024-01-01T12:05:00Z"), 1.1050)
            },
            DrawingStyleRules.Default(Accent),
            Locked: false,
            Hidden: false,
            Extra: null);
    }

    private static WorkspaceDocument SampleWorkspace()
    {
        return new WorkspaceDocument
        {
            View = new ChartViewStateDto
            {
                BarSpacing = 8,
                RightOffset = 5,
                Symbol = "EURUSD",
                Timeframe = Timeframe.M5,
            },
            Drawings =
            {
                Drawing("d1"),
                Drawing("d2"),
                Drawing("d3")
            },
            Indicators =
            {
                new IndicatorInstance("ind1", "SMA", 14, new Dictionary<string, double> { ["Period"] = 14 })
            }
        };
    }

    [Fact]
    public void RoundTrip_ThreeDrawingsOneIndicator_ByteStableOnSecondSave()
    {
        var doc = SampleWorkspace();
        var json1 = doc.Save();
        var loaded = WorkspaceDocument.Load(json1, isKnownDrawingType: (tid, ver) => tid.StartsWith("test."));
        var json2 = loaded.Save();
        var loaded2 = WorkspaceDocument.Load(json2, isKnownDrawingType: (tid, ver) => tid.StartsWith("test."));
        var json3 = loaded2.Save();

        Assert.Equal(json2, json3);
        Assert.Equal(Encoding.UTF8.GetBytes(json2), Encoding.UTF8.GetBytes(json3));

        Assert.Equal(3, loaded2.Drawings.Count);
        Assert.Single(loaded2.Indicators);
        Assert.Equal("EURUSD", loaded2.View.Symbol);
        Assert.Equal(Timeframe.M5, loaded2.View.Timeframe);
        Assert.Equal(1.1000, loaded2.Drawings[0].Anchors[0].Price, 10);
    }

    [Fact]
    public void UnknownDrawing_WrittenBackByteIdentical()
    {
        var unknownRaw = System.Text.Json.Nodes.JsonNode.Parse("""
            {
              "id": "u1",
              "typeId": "future.tool",
              "typeVersion": 9,
              "anchors": [ { "timeUtc": "2024-01-01T00:00:00.0000000Z", "price": 1.2 } ],
              "style": { "color": 4278219391, "thickness": 2 },
              "locked": false,
              "hidden": false,
              "extra": { "note": "keep-me" }
            }
            """)!.AsObject();

        var doc = new WorkspaceDocument
        {
            Drawings = { Drawing("known1") },
            UnknownDrawings =
            {
                new UnknownDrawingObject("u1", "future.tool", 9, null, unknownRaw)
            }
        };

        var json = doc.Save();
        var loaded = WorkspaceDocument.Load(json, isKnownDrawingType: (tid, _) => tid != "future.tool");
        Assert.Single(loaded.Drawings);
        Assert.Single(loaded.UnknownDrawings);
        Assert.Equal("future.tool", loaded.UnknownDrawings[0].TypeId);

        var json2 = loaded.Save();
        var loaded2 = WorkspaceDocument.Load(json2, isKnownDrawingType: (tid, _) => tid != "future.tool");
        Assert.Equal(loaded.UnknownDrawings[0].TypeId, loaded2.UnknownDrawings[0].TypeId);
        Assert.Equal(loaded.UnknownDrawings[0].TypeVersion, loaded2.UnknownDrawings[0].TypeVersion);
        // second save stable
        Assert.Equal(json2, loaded2.Save());
    }

    [Fact]
    public void NewerMajorVersion_ThrowsSerializationException()
    {
        var json = """
            {
              "schema": "mychart.workspace",
              "version": 99,
              "view": {}
            }
            """;
        var ex = Assert.Throws<SerializationException>(() => WorkspaceDocument.Load(json));
        Assert.Contains("99", ex.Message);
    }

    [Fact]
    public void DrawingData_CategoryHeaderPresent()
    {
        var json = DrawingDataSerializer.Serialize(new[] { Drawing("a") }, Array.Empty<UnknownDrawingObject>());
        Assert.Contains("mychart.drawingdata", json);
        Assert.Contains("\"version\": 1", json);
    }
}
