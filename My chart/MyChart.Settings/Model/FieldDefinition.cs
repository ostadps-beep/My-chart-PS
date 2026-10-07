namespace MyChart.Settings.Model;

public sealed class CategoryDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
}

public sealed class FieldDefinition
{
    public required string Key { get; init; }
    public required string CategoryId { get; init; }
    public required string Group { get; init; }
    public required string Label { get; init; }
    public required ControlKind Control { get; init; }
    public required object DefaultValue { get; init; }
    public ApplyMode ApplyMode { get; init; } = ApplyMode.Immediate;
    public double? Min { get; init; }
    public double? Max { get; init; }
    public double Increment { get; init; } = 1;
    public IReadOnlyList<string>? Options { get; init; }
    public string? EnableWhen { get; init; }
    public string? CommandHint { get; init; }
}

public sealed class ParameterEntry
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
}

