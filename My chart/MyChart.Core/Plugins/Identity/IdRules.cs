using System.Text.RegularExpressions;

namespace MyChart.Core.Plugins.Identity;

/// <summary>Ids: PascalCase alphanumeric, min length 3 (e.g. TrendLine). AT5 V1.</summary>
public static partial class IdRules
{
    [GeneratedRegex("^[A-Z][A-Za-z0-9]{2,}$", RegexOptions.CultureInvariant)]
    private static partial Regex PascalId();

    public static bool IsValid(string id) => !string.IsNullOrEmpty(id) && PascalId().IsMatch(id);

    public static string? Validate(string id) =>
        IsValid(id) ? null : Manifest.ErrorCodes.InvalidId;
}
