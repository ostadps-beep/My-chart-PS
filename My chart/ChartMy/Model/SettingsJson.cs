using System.Text.Json;
using System.Text.Json.Nodes;

namespace ChartMy.Model;

public static class SettingsJson
{
    public static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true
    };

    public static string ToJson(SettingsDocument document)
    {
        var root = new JsonObject();
        foreach (var field in SettingsSchema.Fields)
        {
            if (field.Control == ControlKind.Command)
                continue;
            if (!document.TryGetNode(field.Key, out var node))
                continue;
            WriteKey(root, field.Key, node?.DeepClone());
        }

        var wrapper = new JsonObject { [SettingsSchema.RootName] = root };
        return wrapper.ToJsonString(WriteOptions);
    }

    public static SettingsDocument FromJson(string json)
    {
        var doc = SettingsDocument.FromDefaults();
        if (string.IsNullOrWhiteSpace(json))
            return doc;

        var parsed = JsonNode.Parse(json);
        var root = parsed as JsonObject;
        if (root is not null && root.TryGetPropertyValue(SettingsSchema.RootName, out var inner) && inner is JsonObject nested)
            root = nested;

        if (root is null)
            return doc;

        foreach (var field in SettingsSchema.Fields)
        {
            if (field.Control == ControlKind.Command)
                continue;
            if (TryReadKey(root, field.Key, out var node) && node is not null)
                doc.SetNode(field.Key, node);
        }

        return doc;
    }

    private static void WriteKey(JsonObject root, string key, JsonNode? node)
    {
        if (!SettingsSchema.TrySplitKey(key, out var section, out var leaf))
            return;

        var sectionObject = GetOrCreatePath(root, section);
        if (string.IsNullOrEmpty(leaf))
            return;
        sectionObject[leaf] = node;
    }

    private static bool TryReadKey(JsonObject root, string key, out JsonNode? node)
    {
        node = null;
        if (!SettingsSchema.TrySplitKey(key, out var section, out var leaf))
            return false;

        if (!TryGetPath(root, section, out var sectionNode) || sectionNode is not JsonObject sectionObject)
            return false;

        return sectionObject.TryGetPropertyValue(leaf, out node);
    }

    private static JsonObject GetOrCreatePath(JsonObject root, string dottedPath)
    {
        var current = root;
        foreach (var part in dottedPath.Split('.'))
        {
            if (current[part] is JsonObject existing)
            {
                current = existing;
                continue;
            }

            var created = new JsonObject();
            current[part] = created;
            current = created;
        }

        return current;
    }

    private static bool TryGetPath(JsonObject root, string dottedPath, out JsonNode? node)
    {
        JsonNode? current = root;
        foreach (var part in dottedPath.Split('.'))
        {
            if (current is not JsonObject obj || !obj.TryGetPropertyValue(part, out current))
            {
                node = null;
                return false;
            }
        }

        node = current;
        return true;
    }
}
