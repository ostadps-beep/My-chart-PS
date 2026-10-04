namespace MyChart.Core.Contracts.Plugins;

public enum HitKind { None, Body, Handle }

public readonly record struct HitResult(HitKind Kind, int HandleIndex = -1);
