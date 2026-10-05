using System.Text.Json.Nodes;
using MyChart.Core.Plugins.Manifest;

namespace MyChart.Core.Plugins.Vocabulary;

/// <summary>PG2.01 DefinitionValidator — AT14 vectors.</summary>
public static class DefinitionValidator
{
    private static readonly HashSet<string> KnownShapes = new(StringComparer.Ordinal)
    {
        "Line", "Rect", "Ellipse", "Polyline", "Text", "ArrowHead", "LevelLines", "InfoBox"
    };

    private static readonly HashSet<string> AllowedRoot = new(StringComparer.Ordinal)
    {
        "schema", "vocabularyVersion", "anchors", "workflow", "parameters", "named", "shapes"
    };

    public static string? Validate(JsonObject root, out ToolDefinition? definition)
    {
        definition = null;

        foreach (var key in root)
        {
            if (!AllowedRoot.Contains(key.Key))
                return ErrorCodes.DefinitionSchemaError; // unknown property e.g. colour
        }

        var schema = root["schema"]?.GetValue<string>();
        if (schema is not null && schema != "mychart.tooldef")
            return ErrorCodes.DefinitionSchemaError;

        var vocab = root["vocabularyVersion"]?.GetValue<string>() ?? "";
        if (vocab != VocabularyVersions.Current)
            return ErrorCodes.VocabularyUnsupported; // 2.0 or 1.1 vs host 1.0

        int anchors = root["anchors"]?.GetValue<int>() ?? 0;
        if (anchors < 1 || anchors > VocabularyVersions.MaxAnchors)
            return ErrorCodes.DefinitionSchemaError; // 0 or 9

        var workflow = root["workflow"]?.GetValue<string>() ?? "Click";
        if (workflow == "ClickThenText" && anchors != 1)
            return ErrorCodes.DefinitionSchemaError;

        // named: forward references
        var named = new List<NamedExpression>();
        var seenNames = new HashSet<string>(StringComparer.Ordinal);
        if (root["named"] is JsonArray namedArr)
        {
            foreach (var n in namedArr)
            {
                if (n is not JsonObject no) continue;
                var name = no["name"]?.GetValue<string>() ?? "";
                var expr = no["expr"]?.GetValue<string>() ?? "";
                // crude forward-ref: if expr contains named.X and X not yet seen
                foreach (var existing in seenNames)
                {
                    // check later names used - scan for named. laterName
                }
                // Detect using a later name: if expr contains "named." + some name not yet in seenNames
                // We'll check after collecting all names is wrong; order matters.
                // For forward ref: if expr mentions named.foo and foo not in seenNames yet
                var idx = 0;
                while ((idx = expr.IndexOf("named.", idx, StringComparison.Ordinal)) >= 0)
                {
                    idx += 6;
                    int end = idx;
                    while (end < expr.Length && (char.IsLetterOrDigit(expr[end]) || expr[end] == '_'))
                        end++;
                    var refName = expr[idx..end];
                    if (!seenNames.Contains(refName))
                        return ErrorCodes.DefinitionSchemaError; // later or undefined name
                }
                seenNames.Add(name);
                named.Add(new NamedExpression(name, expr));
            }
        }

        var shapes = new List<JsonObject>();
        if (root["shapes"] is JsonArray shapesArr)
        {
            if (shapesArr.Count > VocabularyVersions.MaxShapes)
                return ErrorCodes.LimitExceeded; // 17 shapes
            foreach (var s in shapesArr)
            {
                if (s is not JsonObject so) continue;
                var kind = so["kind"]?.GetValue<string>() ?? so["type"]?.GetValue<string>() ?? "";
                if (!KnownShapes.Contains(kind))
                    return ErrorCodes.VocabularyUnsupported; // Spiral
                if (kind == "LevelLines")
                {
                    if (so["levels"] is JsonArray levels && levels.Count > VocabularyVersions.MaxLevels)
                        return ErrorCodes.LimitExceeded;
                }
                // Point anchor out of range
                if (ContainsOutOfRangeAnchor(so, anchors))
                    return ErrorCodes.DefinitionSchemaError;
                shapes.Add(so);
            }
        }

        definition = new ToolDefinition
        {
            Schema = schema ?? "mychart.tooldef",
            VocabularyVersion = vocab,
            Anchors = anchors,
            Workflow = workflow,
            Named = named,
            Shapes = shapes,
            Raw = root
        };
        return null;
    }

    public static string? ValidateJson(string json, out ToolDefinition? definition)
    {
        definition = null;
        JsonObject? root;
        try
        {
            root = JsonNode.Parse(json) as JsonObject;
        }
        catch
        {
            return ErrorCodes.DefinitionSchemaError;
        }
        if (root is null) return ErrorCodes.DefinitionSchemaError;
        return Validate(root, out definition);
    }

    private static bool ContainsOutOfRangeAnchor(JsonNode node, int anchors)
    {
        if (node is JsonObject o)
        {
            if (o.TryGetPropertyValue("anchor", out var a) && a is JsonValue jv)
            {
                int idx = jv.GetValue<int>();
                if (idx < 0 || idx >= anchors) return true;
            }
            foreach (var kv in o)
            {
                if (kv.Value is not null && ContainsOutOfRangeAnchor(kv.Value, anchors))
                    return true;
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                if (item is not null && ContainsOutOfRangeAnchor(item, anchors))
                    return true;
            }
        }
        return false;
    }
}
