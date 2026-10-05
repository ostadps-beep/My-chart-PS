using System.Text.Json.Nodes;
using MyChart.Core.Commands;

namespace MyChart.Core.Serialization;

public static class IndicatorDataSerializer
{
    public const string Schema = "mychart.indicatordata";
    public const int Version = 1;

    public static string Serialize(IReadOnlyList<IndicatorInstance> indicators)
    {
        var items = new JsonArray();
        foreach (var ind in indicators)
        {
            var pars = new JsonObject();
            foreach (var kv in ind.Parameters.OrderBy(k => k.Key, StringComparer.Ordinal))
                pars[kv.Key] = kv.Value;

            items.Add(new JsonObject
            {
                ["id"] = ind.Id,
                ["indicatorName"] = ind.IndicatorName,
                ["parameterHash"] = ind.ParameterHash,
                ["parameters"] = pars
            });
        }

        var payload = new JsonObject { ["indicators"] = items };
        var root = CategoryEnvelope.Wrap(Schema, Version, payload);
        return root.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }

    public static List<IndicatorInstance> Deserialize(string json)
    {
        var root = JsonNode.Parse(json) as JsonObject
            ?? throw new SerializationException("IndicatorData root must be an object.");
        var (schema, version, _) = CategoryEnvelope.ReadHeader(root);
        if (!string.Equals(schema, Schema, StringComparison.Ordinal))
            throw new SerializationException($"Expected schema '{Schema}', got '{schema}'.");
        CategoryEnvelope.EnsureMajorSupported(schema, version);

        var list = new List<IndicatorInstance>();
        if (root["indicators"] is not JsonArray arr)
            return list;

        foreach (var n in arr)
        {
            if (n is not JsonObject o) continue;
            var id = o["id"]?.GetValue<string>() ?? "";
            var name = o["indicatorName"]?.GetValue<string>() ?? "";
            var hash = o["parameterHash"]?.GetValue<int>() ?? 0;
            var pars = new Dictionary<string, double>();
            if (o["parameters"] is JsonObject po)
            {
                foreach (var kv in po)
                {
                    if (kv.Value is JsonValue jv)
                        pars[kv.Key] = jv.GetValue<double>();
                }
            }
            list.Add(new IndicatorInstance(id, name, hash, pars));
        }
        return list;
    }
}
