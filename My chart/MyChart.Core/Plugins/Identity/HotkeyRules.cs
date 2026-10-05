namespace MyChart.Core.Plugins.Identity;

/// <summary>AT5 V2: Esc, Ctrl+Z reserved; Ctrl+Q and single letter ok.</summary>
public static class HotkeyRules
{
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "Esc", "Escape", "Ctrl+Z", "Control+Z", "Ctrl+Y", "Control+Y", "Ctrl+Shift+Z"
    };

    public static bool IsReserved(string? hotkey)
    {
        if (string.IsNullOrWhiteSpace(hotkey)) return false;
        return Reserved.Contains(hotkey.Trim());
    }

    public static string? Validate(string? hotkey) =>
        IsReserved(hotkey) ? Manifest.ErrorCodes.HotkeyReserved : null;
}
