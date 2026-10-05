using System.Text.RegularExpressions;

namespace MyChart.Core.Plugins.Identity;

/// <summary>AT5 V6: SVG path subset M/L commands; max 4096 chars.</summary>
public static partial class GeometryPathGrammar
{
    private const int MaxLength = 4096;

    [GeneratedRegex(@"^\s*([MmLl](\s+-?\d+(\.\d+)?\s+-?\d+(\.\d+)?)+)\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex SimplePath();

    public static bool IsValid(string? pathData)
    {
        if (string.IsNullOrWhiteSpace(pathData)) return false;
        if (pathData.Length > MaxLength) return false;
        // Allow multi-command paths with spaces
        var normalized = pathData.Trim();
        // Reject unknown command letters other than M m L l Z z H h V v
        foreach (char c in normalized)
        {
            if (char.IsLetter(c) && !"MmLlZzHhVvCcSsQqTtAa".Contains(c))
                return false;
        }
        // Must start with M/m
        var first = normalized.SkipWhile(char.IsWhiteSpace).FirstOrDefault();
        return first is 'M' or 'm';
    }

    public static string? Validate(string? pathData) =>
        IsValid(pathData) ? null : Manifest.ErrorCodes.InvalidGeometry;
}
