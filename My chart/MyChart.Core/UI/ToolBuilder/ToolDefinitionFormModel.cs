using System.Text.Json.Nodes;
using MyChart.Core.Plugins.Vocabulary;

namespace MyChart.Core.UI.ToolBuilder;

/// <summary>One shape row in the builder form (no raw JSON typing).</summary>
public sealed class ShapeFormItem
{
    public string Kind { get; set; } = "Line";
    public int FromAnchor { get; set; }
    public int ToAnchor { get; set; } = 1;
    public string? ExpressionError { get; set; }
}

/// <summary>One parameter row.</summary>
public sealed class ParameterFormItem
{
    public string Key { get; set; } = "period";
    public string DisplayName { get; set; } = "Period";
    public double DefaultValue { get; set; } = 14;
    public double? Min { get; set; } = 1;
    public double? Max { get; set; } = 500;
}

/// <summary>
/// PG6.01 BuilderShellAndForm — form model from vocabulary.
/// Fields: name, category, anchors, workflow, placements, hotkey, parameters, shapes.
/// Invalid input never becomes a saveable definition (Validate blocks Save).
/// </summary>
public sealed class ToolDefinitionFormModel
{
    public string Name { get; set; } = "";
    public string Category { get; set; } = "Drawing";
    public int Anchors { get; set; } = 2;
    public string Workflow { get; set; } = "Click";
    public string Hotkey { get; set; } = "";
    public int LeftToolbarOrder { get; set; } = 200;
    public List<ParameterFormItem> Parameters { get; } = new();
    public List<ShapeFormItem> Shapes { get; } = new();

    public string? ValidationError { get; private set; }
    public bool CanSave => string.IsNullOrEmpty(ValidationError) && !string.IsNullOrWhiteSpace(Name);

    public void AddShape(string kind = "Line")
    {
        Shapes.Add(new ShapeFormItem
        {
            Kind = kind,
            FromAnchor = 0,
            ToAnchor = Math.Max(0, Anchors - 1)
        });
    }

    public bool RemoveShape(int index)
    {
        if (index < 0 || index >= Shapes.Count) return false;
        Shapes.RemoveAt(index);
        return true;
    }

    public bool MoveShape(int index, int delta)
    {
        var target = index + delta;
        if (index < 0 || index >= Shapes.Count || target < 0 || target >= Shapes.Count)
            return false;
        (Shapes[index], Shapes[target]) = (Shapes[target], Shapes[index]);
        return true;
    }

    public void AddParameter()
    {
        Parameters.Add(new ParameterFormItem());
    }

    public bool RemoveParameter(int index)
    {
        if (index < 0 || index >= Parameters.Count) return false;
        Parameters.RemoveAt(index);
        return true;
    }

    /// <summary>Build Definition.json object and validate with DefinitionValidator.</summary>
    public bool Validate()
    {
        ValidationError = null;
        if (string.IsNullOrWhiteSpace(Name))
        {
            ValidationError = "Name is required.";
            return false;
        }

        if (Anchors < 1 || Anchors > 8)
        {
            ValidationError = "Anchors must be between 1 and 8.";
            return false;
        }

        if (Workflow == "ClickThenText" && Anchors != 1)
        {
            ValidationError = "ClickThenText requires exactly 1 anchor.";
            return false;
        }

        var json = BuildDefinitionJson();
        var err = DefinitionValidator.ValidateJson(json, out _);
        if (err is not null)
        {
            ValidationError = err;
            return false;
        }

        return true;
    }

    public string BuildDefinitionJson()
    {
        var shapes = new JsonArray();
        foreach (var s in Shapes)
        {
            shapes.Add(BuildShapeObject(s));
        }

        var parameters = new JsonArray();
        foreach (var p in Parameters)
        {
            var po = new JsonObject
            {
                ["key"] = p.Key,
                ["displayName"] = p.DisplayName,
                ["default"] = p.DefaultValue
            };
            if (p.Min is not null) po["min"] = p.Min;
            if (p.Max is not null) po["max"] = p.Max;
            parameters.Add(po);
        }

        var root = new JsonObject
        {
            ["schema"] = "mychart.tooldef",
            ["vocabularyVersion"] = VocabularyVersions.Current,
            ["anchors"] = Anchors,
            ["workflow"] = Workflow,
            ["shapes"] = shapes
        };
        if (parameters.Count > 0)
            root["parameters"] = parameters;

        return root.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>Try get definition JSON only when valid; otherwise null (never write invalid to disk).</summary>
    public string? TryGetSaveableDefinition()
    {
        if (!Validate())
            return null;
        return BuildDefinitionJson();
    }

    public static ToolDefinitionFormModel FromTrendLineTemplate()
    {
        var m = new ToolDefinitionFormModel
        {
            Name = "TrendLine",
            Category = "Drawing",
            Anchors = 2,
            Workflow = "Click"
        };
        m.Shapes.Add(new ShapeFormItem { Kind = "Line", FromAnchor = 0, ToAnchor = 1 });
        return m;
    }

    private static JsonObject BuildShapeObject(ShapeFormItem s)
    {
        return s.Kind switch
        {
            "Rect" => new JsonObject
            {
                ["kind"] = "Rect",
                ["a"] = AnchorPoint(s.FromAnchor),
                ["b"] = AnchorPoint(s.ToAnchor)
            },
            "Line" or _ => new JsonObject
            {
                ["kind"] = "Line",
                ["from"] = AnchorPoint(s.FromAnchor),
                ["to"] = AnchorPoint(s.ToAnchor),
                ["extend"] = "none"
            }
        };
    }

    private static JsonObject AnchorPoint(int anchor) => new()
    {
        ["x"] = new JsonObject { ["anchor"] = anchor },
        ["y"] = new JsonObject { ["anchor"] = anchor }
    };
}
