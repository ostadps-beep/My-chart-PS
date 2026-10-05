namespace MyChart.Core.Models.Indicators;

/// <summary>Typed indicator input with default and optional range.</summary>
public sealed record IndicatorParameter(
    string Key,
    string DisplayName,
    double DefaultValue,
    double? Min = null,
    double? Max = null,
    double Step = 1.0);
