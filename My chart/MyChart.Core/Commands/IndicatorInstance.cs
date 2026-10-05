namespace MyChart.Core.Commands;

/// <summary>Lightweight indicator placement on a chart (full model in T3.05 / later UI).</summary>
public sealed record IndicatorInstance(
    string Id,
    string IndicatorName,
    int ParameterHash,
    IReadOnlyDictionary<string, double> Parameters);
