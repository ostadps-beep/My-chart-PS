using System.Text.RegularExpressions;

namespace MyChart.Core.Plugins.Identity;

public static partial class SemVer
{
    [GeneratedRegex(@"^\d+\.\d+\.\d+$", RegexOptions.CultureInvariant)]
    private static partial Regex ThreePart();

    /// <summary>AT5 V8: "1.0.0" ok; "1.0" invalid (E018).</summary>
    public static bool IsValidThreePart(string version) =>
        !string.IsNullOrEmpty(version) && ThreePart().IsMatch(version);

    public static string? ValidateThreePart(string version) =>
        IsValidThreePart(version) ? null : Manifest.ErrorCodes.VersionInvalid;

    /// <summary>Contract version major.minor compatibility (host 1.0 accepts 1.x not 2.0).</summary>
    public static bool IsContractCompatible(string component, string host)
    {
        if (!TryParseMajorMinor(component, out int cMaj, out int cMin)) return false;
        if (!TryParseMajorMinor(host, out int hMaj, out int hMin)) return false;
        if (cMaj != hMaj) return false;
        return cMin <= hMin || cMaj == hMaj; // same major; minor can be lower or equal preferred
    }

    public static bool TryParseMajorMinor(string v, out int major, out int minor)
    {
        major = 0; minor = 0;
        var parts = v.Split('.');
        if (parts.Length < 2) return false;
        return int.TryParse(parts[0], out major) && int.TryParse(parts[1], out minor);
    }
}
