namespace MyChart.Core.UI.Menus;

/// <summary>
/// T6.02 — every menu item maps to one command or one state setter.
/// Host wires Command / Toggle / SetState handlers; this class only dispatches by kind.
/// </summary>
public sealed class ContextMenuInvoker
{
    private readonly Action<string> _executeCommand;
    private readonly Action<string, bool> _setToggle;
    private readonly Action<string, string?> _setState;

    public ContextMenuInvoker(
        Action<string> executeCommand,
        Action<string, bool> setToggle,
        Action<string, string?> setState)
    {
        _executeCommand = executeCommand ?? throw new ArgumentNullException(nameof(executeCommand));
        _setToggle = setToggle ?? throw new ArgumentNullException(nameof(setToggle));
        _setState = setState ?? throw new ArgumentNullException(nameof(setState));
    }

    public void Invoke(ContextMenuItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (!item.IsEnabled)
            return;

        switch (item.Kind)
        {
            case MenuActionKind.Command:
                _executeCommand(item.Target);
                break;
            case MenuActionKind.Toggle:
                _setToggle(item.Target, !item.IsChecked);
                break;
            case MenuActionKind.SetState:
                _setState(item.Target, item.Value);
                break;
        }
    }
}
