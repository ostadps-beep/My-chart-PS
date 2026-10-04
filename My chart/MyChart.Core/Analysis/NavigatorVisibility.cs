namespace MyChart.Core.Analysis;

/// <summary>
/// T3.03 PANEL visibility: default hidden; once shown, the panel hides itself 5 s after the last interaction.
/// Time is passed in, so the rule is deterministic. An interaction while the panel is hidden does not show it.
/// </summary>
public sealed class NavigatorVisibility
{
    public static readonly TimeSpan AutoHideAfter = TimeSpan.FromSeconds(5);

    private DateTimeOffset? _visibleUntil;

    /// <summary>The user invoked the navigator (shortcut, button or menu).</summary>
    public void Show(DateTimeOffset now) => _visibleUntil = now + AutoHideAfter;

    /// <summary>The user hid the panel explicitly.</summary>
    public void Hide() => _visibleUntil = null;

    /// <summary>Any interaction with the panel while it is visible restarts the 5 s timer.</summary>
    public void Interact(DateTimeOffset now)
    {
        if (IsVisible(now))
            _visibleUntil = now + AutoHideAfter;
    }

    public bool IsVisible(DateTimeOffset now)
        => _visibleUntil.HasValue && now < _visibleUntil.Value;
}
