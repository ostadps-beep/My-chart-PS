using System.Globalization;
using System.Text.Json.Nodes;
using MyChart.Core.Models.Drawing;
using MyChart.Core.Models.Rendering;

namespace MyChart.Core.Serialization;

/// <summary>
/// T3.07 DrawingData. DRAWING_RECORD = {id, typeId, typeVersion, anchors[{timeUtc, price}], style, locked, hidden, extra}.
/// Unknown typeId / newer typeVersion -> UnknownDrawingObject, written back byte-identical.
/// </summary>
public static class DrawingDataSerializer
{
    public const string Schema = "mychart.drawingdata";
    public const int Version = 1;

    public static string Serialize(IReadOnlyList<DrawingObject> known, IReadOnlyList<UnknownDrawingObject> unknown)
    {
        var items = new JsonArray();
        foreach (var d in known)
            items.Add(WriteKnown(d));
        foreach (var u in unknown)
            items.Add(u.RawJson?.DeepClone() as JsonObject ?? WriteUnknownFallback(u));

        var payload = new JsonObject { ["drawings"] = items };
        var root = CategoryEnvelope.Wrap(Schema, Version, payload);
        return root.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }

    public static (List<DrawingObject> Known, List<UnknownDrawingObject> Unknown) Deserialize(
        string json,
        Func<string, int, bool>? isKnownType = null)
    {
        isKnownType ??= static (_, _) => true; // without registry, treat all as known if parseable

        var root = JsonNode.Parse(json) as JsonObject
            ?? throw new SerializationException("DrawingData root must be an object.");
        var (schema, version, _) = CategoryEnvelope.ReadHeader(root);
        if (!string.Equals(schema, Schema, StringComparison.Ordinal))
            throw new SerializationException($"Expected schema '{Schema}', got '{schema}'.");
        CategoryEnvelope.EnsureMajorSupported(schema, version);

        var known = new List<DrawingObject>();
        var unknown = new List<UnknownDrawingObject>();
        var arr = root["drawings"] as JsonArray ?? new JsonArray();

        foreach (var node in arr)
        {
            if (node is not JsonObject obj)
                continue;

            var typeId = obj["typeId"]?.GetValue<string>() ?? "";
            var typeVersion = obj["typeVersion"]?.GetValue<int>() ?? 0;
            var id = obj["id"]?.GetValue<string>() ?? "";

            bool knownType = isKnownType(typeId, typeVersion);
            if (!knownType)
            {
                unknown.Add(new UnknownDrawingObject(
                    id, typeId, typeVersion,
                    obj["extra"] as JsonObject,
                    (JsonObject)obj.DeepClone()!));
                continue;
            }

            try
            {
                known.Add(ReadKnown(obj));
            }
            catch
            {
                unknown.Add(new UnknownDrawingObject(
                    id, typeId, typeVersion,
                    obj["extra"] as JsonObject,
                    (JsonObject)obj.DeepClone()!));
            }
        }

        return (known, unknown);
    }

    private static JsonObject WriteKnown(DrawingObject d)
    {
        var anchors = new JsonArray();
        foreach (var a in d.Anchors)
        {
            anchors.Add(new JsonObject
            {
                ["timeUtc"] = a.TimeUtc.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture),
                ["price"] = JsonValue.Create(a.Price)
            });
        }

        var style = new JsonObject
        {
            ["color"] = d.Style.Color.Argb,
            ["thickness"] = d.Style.Thickness,
            ["showLabels"] = d.Style.ShowLabels,
            ["opacity"] = d.Style.Opacity,
            ["lineStyle"] = d.Style.LineStyle.ToString()
        };

        var o = new JsonObject
        {
            ["id"] = d.Id,
            ["typeId"] = d.TypeId,
            ["typeVersion"] = d.TypeVersion,
            ["anchors"] = anchors,
            ["style"] = style,
            ["locked"] = d.Locked,
            ["hidden"] = d.Hidden
        };
        if (d.Extra is not null)
            o["extra"] = d.Extra.DeepClone();
        return o;
    }

    private static JsonObject WriteUnknownFallback(UnknownDrawingObject u)
    {
        return new JsonObject
        {
            ["id"] = u.Id,
            ["typeId"] = u.TypeId,
            ["typeVersion"] = u.TypeVersion,
            ["anchors"] = new JsonArray(),
            ["style"] = new JsonObject(),
            ["locked"] = false,
            ["hidden"] = true,
            ["extra"] = u.Extra?.DeepClone()
        };
    }

    private static DrawingObject ReadKnown(JsonObject obj)
    {
        var id = obj["id"]?.GetValue<string>() ?? throw new SerializationException("Drawing missing id.");
        var typeId = obj["typeId"]?.GetValue<string>() ?? "";
        var typeVersion = obj["typeVersion"]?.GetValue<int>() ?? 1;
        var locked = obj["locked"]?.GetValue<bool>() ?? false;
        var hidden = obj["hidden"]?.GetValue<bool>() ?? false;

        var anchors = new List<DrawingAnchor>();
        if (obj["anchors"] is JsonArray arr)
        {
            foreach (var n in arr)
            {
                if (n is not JsonObject a) continue;
                var t = DateTimeOffset.Parse(a["timeUtc"]!.GetValue<string>(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                var price = a["price"]!.GetValue<double>();
                anchors.Add(new DrawingAnchor(t, price));
            }
        }

        DrawingStyle style = new(new RgbaColor(0xFF2196F3), 1.0);
        if (obj["style"] is JsonObject s)
        {
            uint color = s["color"]?.GetValue<uint>() ?? 0xFF2196F3;
            double thickness = s["thickness"]?.GetValue<double>() ?? 1.0;
            bool showLabels = s["showLabels"]?.GetValue<bool>() ?? true;
            double opacity = s["opacity"]?.GetValue<double>() ?? 1.0;
            var lineStyle = DrawingLineStyle.Solid;
            if (s["lineStyle"] is JsonValue ls && Enum.TryParse<DrawingLineStyle>(ls.GetValue<string>(), true, out var parsed))
                lineStyle = parsed;
            style = new DrawingStyle(new RgbaColor(color), thickness, showLabels, opacity, lineStyle);
        }

        var extra = obj["extra"] as JsonObject;
        return new DrawingObject(id, typeId, typeVersion, anchors, style, locked, hidden, extra);
    }
}
