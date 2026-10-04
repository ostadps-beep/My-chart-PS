namespace MyChart.Core.Contracts.Plugins;

public enum PointerKind { Down, Move, Up }

public readonly record struct PointerEvent(
    PointerKind Kind,
    string Button,
    double X,
    double Y,
    string Modifiers);
