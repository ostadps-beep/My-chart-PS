namespace MyChart.Interaction.Input;

public enum PointerButton
{
    None,
    Left,
    Middle,
    Right
}

public enum PointerPhase
{
    Move,
    Down,
    Up,
    Wheel,
    DoubleClick
}

public readonly record struct PointerInput(
    PointerPhase Phase,
    PointerButton Button,
    double X,
    double Y,
    double WheelDelta,
    bool Ctrl,
    bool Shift,
    bool Space);
