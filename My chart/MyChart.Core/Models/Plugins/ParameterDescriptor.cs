namespace MyChart.Core.Models.Plugins;

public enum ParameterKind
{
    Color,
    Number,
    Choice,
    Toggle,
    Text,
    NumberList
}

public sealed record ParameterDescriptor(
    string Key,
    string Label,
    ParameterKind Kind,
    string Default,
    double? Min = null,
    double? Max = null,
    IReadOnlyList<string>? Options = null);
