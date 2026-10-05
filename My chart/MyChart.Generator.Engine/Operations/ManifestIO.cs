using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MyChart.Core.Plugins.Manifest;

namespace MyChart.Generator.Engine.Operations;

/// <summary>
/// PG3.02 ManifestIO — deterministic write (fixed property order, indented, LF, trailing newline);
/// sha256 over bytes with LF normalisation; unknown property = E015.
/// </summary>
public static class ManifestIO
{
    private static readonly string[] KnownProperties =
    {
        "componentId", "kind", "name", "version", "contractVersion",
        "toolId", "typeId", "iconKey", "hotkey", "category",
        "anchors", "active", "order", "slot", "dependsOn"
    };

    public static ComponentManifest Read(string json)
    {
        var node = JsonNode.Parse(json) as JsonObject
            ?? throw new ManifestException(ErrorCodes.ManifestSchemaError, "Root must be an object");

        foreach (var prop in node)
        {
            if (!KnownProperties.Contains(prop.Key, StringComparer.Ordinal))
                throw new ManifestException(ErrorCodes.ManifestSchemaError, $"Unknown property '{prop.Key}'");
        }

        return new ComponentManifest
        {
            ComponentId = GetString(node, "componentId") ?? "",
            Kind = GetString(node, "kind") ?? "DataTool",
            Name = GetString(node, "name") ?? "",
            Version = GetString(node, "version") ?? "1.0.0",
            ContractVersion = GetString(node, "contractVersion") ?? "1.0",
            ToolId = GetString(node, "toolId"),
            TypeId = GetString(node, "typeId"),
            IconKey = GetString(node, "iconKey"),
            Hotkey = GetString(node, "hotkey"),
            Category = GetString(node, "category") ?? "Drawing",
            Anchors = GetInt(node, "anchors") ?? 2,
            Active = GetBool(node, "active") ?? true,
            Order = GetInt(node, "order") ?? 0,
            Slot = GetString(node, "slot"),
            DependsOn = GetStringArray(node, "dependsOn")
        };
    }

    public static ComponentManifest ReadFile(string path)
        => Read(File.ReadAllText(path));

    /// <summary>Deterministic JSON: fixed order, 2-space indent, LF, trailing newline.</summary>
    public static string Write(ComponentManifest m)
    {
        var sb = new StringBuilder();
        sb.AppendLine("{");
        WriteProp(sb, "componentId", m.ComponentId, trailingComma: true);
        WriteProp(sb, "kind", m.Kind, trailingComma: true);
        WriteProp(sb, "name", m.Name, trailingComma: true);
        WriteProp(sb, "version", m.Version, trailingComma: true);
        WriteProp(sb, "contractVersion", m.ContractVersion, trailingComma: true);
        if (m.ToolId is not null) WriteProp(sb, "toolId", m.ToolId, trailingComma: true);
        if (m.TypeId is not null) WriteProp(sb, "typeId", m.TypeId, trailingComma: true);
        if (m.IconKey is not null) WriteProp(sb, "iconKey", m.IconKey, trailingComma: true);
        if (m.Hotkey is not null) WriteProp(sb, "hotkey", m.Hotkey, trailingComma: true);
        WriteProp(sb, "category", m.Category, trailingComma: true);
        WritePropRaw(sb, "anchors", m.Anchors.ToString(), trailingComma: true);
        WritePropRaw(sb, "active", m.Active ? "true" : "false", trailingComma: true);
        WritePropRaw(sb, "order", m.Order.ToString(), trailingComma: m.Slot is not null || m.DependsOn.Count > 0);
        if (m.Slot is not null)
            WriteProp(sb, "slot", m.Slot, trailingComma: m.DependsOn.Count > 0);
        if (m.DependsOn.Count > 0)
        {
            sb.Append("  \"dependsOn\": [");
            for (int i = 0; i < m.DependsOn.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(JsonSerializer.Serialize(m.DependsOn[i]));
            }
            sb.AppendLine("]");
        }
        // ensure last property line has no trailing issues — strip trailing comma if present
        var text = sb.ToString();
        // rebuild cleanly without trailing-comma bugs
        return BuildDeterministic(m);
    }

    private static string BuildDeterministic(ComponentManifest m)
    {
        var lines = new List<string>();
        void S(string key, string value) => lines.Add($"  {JsonSerializer.Serialize(key)}: {JsonSerializer.Serialize(value)}");
        void R(string key, string raw) => lines.Add($"  {JsonSerializer.Serialize(key)}: {raw}");

        S("componentId", m.ComponentId);
        S("kind", m.Kind);
        S("name", m.Name);
        S("version", m.Version);
        S("contractVersion", m.ContractVersion);
        if (m.ToolId is not null) S("toolId", m.ToolId);
        if (m.TypeId is not null) S("typeId", m.TypeId);
        if (m.IconKey is not null) S("iconKey", m.IconKey);
        if (m.Hotkey is not null) S("hotkey", m.Hotkey);
        S("category", m.Category);
        R("anchors", m.Anchors.ToString());
        R("active", m.Active ? "true" : "false");
        R("order", m.Order.ToString());
        if (m.Slot is not null) S("slot", m.Slot);
        if (m.DependsOn.Count > 0)
        {
            var arr = "[" + string.Join(", ", m.DependsOn.Select(d => JsonSerializer.Serialize(d))) + "]";
            R("dependsOn", arr);
        }

        var body = string.Join(",\n", lines);
        return "{\n" + body + "\n}\n";
    }

    public static void WriteFile(string path, ComponentManifest m)
    {
        var text = Write(m);
        // Always LF
        text = text.Replace("\r\n", "\n").Replace('\r', '\n');
        if (!text.EndsWith('\n')) text += "\n";
        File.WriteAllText(path, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    /// <summary>SHA-256 of content with CRLF normalised to LF.</summary>
    public static string Sha256Normalized(string content)
    {
        var normalized = content.Replace("\r\n", "\n").Replace('\r', '\n');
        var bytes = Encoding.UTF8.GetBytes(normalized);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string Sha256File(string path)
        => Sha256Normalized(File.ReadAllText(path));

    private static void WriteProp(StringBuilder sb, string key, string value, bool trailingComma)
    {
        sb.Append("  ");
        sb.Append(JsonSerializer.Serialize(key));
        sb.Append(": ");
        sb.Append(JsonSerializer.Serialize(value));
        if (trailingComma) sb.Append(',');
        sb.Append('\n');
    }

    private static void WritePropRaw(StringBuilder sb, string key, string raw, bool trailingComma)
    {
        sb.Append("  ");
        sb.Append(JsonSerializer.Serialize(key));
        sb.Append(": ");
        sb.Append(raw);
        if (trailingComma) sb.Append(',');
        sb.Append('\n');
    }

    private static string? GetString(JsonObject o, string key)
        => o[key]?.GetValue<string>();

    private static int? GetInt(JsonObject o, string key)
    {
        if (o[key] is null) return null;
        try { return o[key]!.GetValue<int>(); }
        catch { return null; }
    }

    private static bool? GetBool(JsonObject o, string key)
    {
        if (o[key] is null) return null;
        try { return o[key]!.GetValue<bool>(); }
        catch { return null; }
    }

    private static List<string> GetStringArray(JsonObject o, string key)
    {
        var list = new List<string>();
        if (o[key] is JsonArray arr)
        {
            foreach (var item in arr)
            {
                var s = item?.GetValue<string>();
                if (s is not null) list.Add(s);
            }
        }
        return list;
    }
}

public sealed class ManifestException : Exception
{
    public string Code { get; }
    public ManifestException(string code, string message) : base(message)
    {
        Code = code;
    }
}
