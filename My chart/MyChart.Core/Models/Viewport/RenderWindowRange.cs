namespace MyChart.Core.Models.Viewport;

/// <summary>T2.03 RenderWindow range.</summary>
public readonly record struct RenderWindowRange(
    int From,
    int To,
    int RenderFrom,
    int RenderTo,
    int VisibleBarCount);
