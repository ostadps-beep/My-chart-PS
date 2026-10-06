using System.Text;
using System.Text.Json.Nodes;
using MyChart.Core.Commands;
using MyChart.Core.Models.Drawing;

namespace MyChart.Core.Serialization;

/// <summary>
/// T3.07 Workspace = LayoutData + DrawingData + IndicatorData + per-chart view state.
/// SettingsData is never included. MarketData is never inside a workspace.
/// T5.04 Favorites = list of canonical names in LayoutData (not Settings).
/// VERIFY: round trip with 3 drawings and 1 indicator is byte-stable on the second save.
/// </summary>
public sealed class WorkspaceDocument
{
    public const string Schema = "mychart.workspace";
    public const int Version = 1;

    public ChartViewStateDto View { get; set; } = new();
    public List<DrawingObject> Drawings { get; set; } = new();
    public List<UnknownDrawingObject> UnknownDrawings { get; set; } = new();
    public List<IndicatorInstance> Indicators { get; set; } = new();

    /// <summary>T5.04 — canonical symbol names; LayoutData only (not Settings).</summary>
    public List<string> Favorites { get; set; } = new();

    public string Save()
    {
        // Nested category payloads (without outer schema when embedded — include schema for independence)
        var drawingJson = DrawingDataSerializer.Serialize(Drawings, UnknownDrawings);
        var indicatorJson = IndicatorDataSerializer.Serialize(Indicators);

        var view = new JsonObject
        {
            ["barSpacing"] = View.BarSpacing,
            ["rightOffset"] = View.RightOffset,
            ["priceScaleFit"] = View.PriceScaleFit.ToString(),
            ["priceScaleTransform"] = View.PriceScaleTransform.ToString(),
            ["manualMin"] = View.ManualMin,
            ["manualMax"] = View.ManualMax,
            ["chartType"] = View.ChartType.ToString(),
            ["symbol"] = View.Symbol,
            ["timeframe"] = View.Timeframe.ToString()
        };

        var favorites = new JsonArray();
        foreach (var name in Favorites)
            favorites.Add(name);

        var root = new JsonObject
        {
            ["schema"] = Schema,
            ["version"] = Version,
            ["view"] = view,
            ["drawingData"] = JsonNode.Parse(drawingJson),
            ["indicatorData"] = JsonNode.Parse(indicatorJson),
            ["favorites"] = favorites
        };

        return root.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }

    public static WorkspaceDocument Load(string json, Func<string, int, bool>? isKnownDrawingType = null)
    {
        var root = JsonNode.Parse(json) as JsonObject
            ?? throw new SerializationException("Workspace root must be an object.");
        var (schema, version, _) = CategoryEnvelope.ReadHeader(root);
        if (!string.Equals(schema, Schema, StringComparison.Ordinal))
            throw new SerializationException($"Expected schema '{Schema}', got '{schema}'.");
        CategoryEnvelope.EnsureMajorSupported(schema, version);

        var doc = new WorkspaceDocument();

        if (root["view"] is JsonObject v)
        {
            doc.View.BarSpacing = v["barSpacing"]?.GetValue<double>() ?? 8;
            doc.View.RightOffset = v["rightOffset"]?.GetValue<double>() ?? 5;
            if (Enum.TryParse<Models.Viewport.ScaleFit>(v["priceScaleFit"]?.GetValue<string>(), true, out var fit))
                doc.View.PriceScaleFit = fit;
            if (Enum.TryParse<Models.Viewport.ScaleTransformKind>(v["priceScaleTransform"]?.GetValue<string>(), true, out var tr))
                doc.View.PriceScaleTransform = tr;
            doc.View.ManualMin = v["manualMin"]?.GetValue<double>() ?? 0;
            doc.View.ManualMax = v["manualMax"]?.GetValue<double>() ?? 1;
            if (Enum.TryParse<Models.Chart.ChartType>(v["chartType"]?.GetValue<string>(), true, out var ct))
                doc.View.ChartType = ct;
            doc.View.Symbol = v["symbol"]?.GetValue<string>() ?? "EURUSD";
            if (Enum.TryParse<Models.Market.Timeframe>(v["timeframe"]?.GetValue<string>(), true, out var tf))
                doc.View.Timeframe = tf;
        }

        if (root["drawingData"] is JsonNode dn)
        {
            var (known, unknown) = DrawingDataSerializer.Deserialize(dn.ToJsonString(), isKnownDrawingType);
            doc.Drawings = known;
            doc.UnknownDrawings = unknown;
        }

        if (root["indicatorData"] is JsonNode ind)
        {
            doc.Indicators = IndicatorDataSerializer.Deserialize(ind.ToJsonString());
        }

        if (root["favorites"] is JsonArray favArr)
        {
            foreach (var item in favArr)
            {
                var name = item?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(name))
                    doc.Favorites.Add(Candles.SymbolRegistry.Normalize(name));
            }
        }

        return doc;
    }

    /// <summary>UTF-8 bytes of Save() for byte-stable comparisons.</summary>
    public byte[] SaveBytes() => Encoding.UTF8.GetBytes(Save());
}
