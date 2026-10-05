using System.Text.Json.Nodes;

namespace MyChart.Core.Serialization;

/// <summary>Every category has {"schema": "...", "version": 1, ...}.</summary>
public static class CategoryEnvelope
{
    public const int CurrentVersion = 1;

    public static JsonObject Wrap(string schema, int version, JsonObject payload)
    {
        var root = new JsonObject
        {
            ["schema"] = schema,
            ["version"] = version
        };
        foreach (var kv in payload)
            root[kv.Key] = kv.Value?.DeepClone();
        return root;
    }

    public static (string Schema, int Version, JsonObject Root) ReadHeader(JsonObject root)
    {
        var schema = root["schema"]?.GetValue<string>()
            ?? throw new SerializationException("Missing 'schema' field.");
        var version = root["version"]?.GetValue<int>()
            ?? throw new SerializationException("Missing 'version' field.");
        return (schema, version, root);
    }

    public static void EnsureMajorSupported(string schema, int version, int supportedMajor = CurrentVersion)
    {
        // Major = version when we use integer 1, 2, ...
        if (version > supportedMajor)
            throw SerializationException.NewerMajorVersion(schema, version, supportedMajor);
    }
}
