namespace MyChart.Interaction.Input;

public readonly record struct KeyInput(
    string Key,
    bool Ctrl,
    bool Shift,
    bool Alt);
