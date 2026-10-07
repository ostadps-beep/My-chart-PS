using System.Globalization;
using System.Text.Json.Nodes;

namespace MyChart.Settings.Model;

public sealed class SettingsDocument
{
    private readonly Dictionary<string, JsonNode?> _values = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, JsonNode?> Values => _values;

    public static SettingsDocument FromDefaults()
    {
        var doc = new SettingsDocument();
        foreach (var field in SettingsSchema.Fields)
        {
            if (field.Control == ControlKind.Command)
                continue;
            doc.Set(field.Key, field.DefaultValue);
        }

        return doc;
    }

    public SettingsDocument Clone()
    {
        var clone = new SettingsDocument();
        foreach (var (key, node) in _values)
            clone._values[key] = node?.DeepClone();
        return clone;
    }

    public void CopyFrom(SettingsDocument other)
    {
        _values.Clear();
        foreach (var (key, node) in other._values)
            _values[key] = node?.DeepClone();
    }

    public bool EqualsValues(SettingsDocument other)
    {
        if (_values.Count != other._values.Count)
            return false;

        foreach (var (key, node) in _values)
        {
            if (!other._values.TryGetValue(key, out var otherNode))
                return false;
            if (!JsonNode.DeepEquals(node, otherNode))
                return false;
        }

        return true;
    }

    public IReadOnlyList<string> DiffKeys(SettingsDocument other)
    {
        var keys = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var key in _values.Keys)
            keys.Add(key);
        foreach (var key in other._values.Keys)
            keys.Add(key);

        return keys
            .Where(key =>
            {
                _values.TryGetValue(key, out var a);
                other._values.TryGetValue(key, out var b);
                return !JsonNode.DeepEquals(a, b);
            })
            .ToList();
    }

    public void Set(string key, object? value) => _values[key] = ToNode(value);

    public void SetNode(string key, JsonNode? node) => _values[key] = node?.DeepClone();

    public bool TryGetNode(string key, out JsonNode? node) => _values.TryGetValue(key, out node);

    public object? Get(string key)
    {
        if (!_values.TryGetValue(key, out var node) || node is null)
            return null;
        return FromNode(node);
    }

    public bool GetBool(string key, bool fallback = false) =>
        Get(key) is bool b ? b : fallback;

    public string GetString(string key, string fallback = "") =>
        Get(key) is string s ? s : fallback;

    public double GetNumber(string key, double fallback = 0)
    {
        return Get(key) switch
        {
            int i => i,
            long l => l,
            double d => d,
            decimal m => (double)m,
            string s when double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) => n,
            _ => fallback
        };
    }

    public int GetInt(string key, int fallback = 0) => (int)Math.Round(GetNumber(key, fallback));

    public List<string> GetStringList(string key)
    {
        if (Get(key) is List<string> list)
            return list;
        return [];
    }

    public List<ParameterEntry> GetParameters(string key)
    {
        if (Get(key) is List<ParameterEntry> list)
            return list;
        return [];
    }

    public static JsonNode? ToNode(object? value)
    {
        switch (value)
        {
            case null:
                return null;
            case JsonNode node:
                return node.DeepClone();
            case bool b:
                return JsonValue.Create(b);
            case int i:
                return JsonValue.Create(i);
            case long l:
                return JsonValue.Create(l);
            case double d:
                return JsonValue.Create(d);
            case float f:
                return JsonValue.Create(f);
            case decimal m:
                return JsonValue.Create(m);
            case string s:
                return JsonValue.Create(s);
            case IEnumerable<string> strings:
            {
                var array = new JsonArray();
                foreach (var item in strings)
                    array.Add(item);
                return array;
            }
            case IEnumerable<ParameterEntry> parameters:
            {
                var array = new JsonArray();
                foreach (var item in parameters)
                {
                    array.Add(new JsonObject
                    {
                        ["name"] = item.Name,
                        ["value"] = item.Value
                    });
                }

                return array;
            }
            default:
                return JsonValue.Create(Convert.ToString(value, CultureInfo.InvariantCulture));
        }
    }

    public static object? FromNode(JsonNode? node)
    {
        switch (node)
        {
            case null:
                return null;
            case JsonValue value:
                if (value.TryGetValue<bool>(out var b))
                    return b;
                if (value.TryGetValue<int>(out var i))
                    return i;
                if (value.TryGetValue<long>(out var l) && l is >= int.MinValue and <= int.MaxValue)
                    return (int)l;
                if (value.TryGetValue<double>(out var d))
                    return d;
                if (value.TryGetValue<string>(out var s))
                    return s;
                return value.ToString();
            case JsonArray array when array.All(IsParameterObject):
                return array.Select(item => new ParameterEntry
                {
                    Name = item?["name"]?.GetValue<string>() ?? "",
                    Value = item?["value"]?.ToString() ?? ""
                }).ToList();
            case JsonArray array:
                return array.Select(item => item?.ToString() ?? "").ToList();
            default:
                return node.ToJsonString();
        }
    }

    private static bool IsParameterObject(JsonNode? node) =>
        node is JsonObject obj && obj.ContainsKey("name");
}

