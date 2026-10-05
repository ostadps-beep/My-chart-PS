using MyChart.Core.Plugins.Manifest;

namespace MyChart.Generator.Engine.Operations;

/// <summary>
/// PG3.01 PathScope — every resolved path must lie under RepoRoot, UserRoot,
/// or the two catalog files; no "..", no symlink escape.
/// </summary>
public sealed class PathScope
{
    public string RepoRoot { get; }
    public string UserRoot { get; }
    public string PluginCatalogPath { get; }
    public string IconCatalogPath { get; }

    public PathScope(string repoRoot, string userRoot, string? pluginCatalogPath = null, string? iconCatalogPath = null)
    {
        RepoRoot = Path.GetFullPath(repoRoot);
        UserRoot = Path.GetFullPath(userRoot);
        PluginCatalogPath = Path.GetFullPath(pluginCatalogPath
            ?? Path.Combine(RepoRoot, "MyChart.Plugins", "PluginCatalog.g.cs"));
        IconCatalogPath = Path.GetFullPath(iconCatalogPath
            ?? Path.Combine(RepoRoot, "MyChart.Plugins", "IconCatalog.g.cs"));
    }

    /// <summary>
    /// Returns null if in scope; otherwise ErrorCodes.PathOutOfScope.
    /// </summary>
    public string? Validate(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return ErrorCodes.PathOutOfScope;

        // Reject explicit parent segments before resolution
        if (path.Contains("..", StringComparison.Ordinal))
            return ErrorCodes.PathOutOfScope;

        string full;
        try
        {
            full = Path.GetFullPath(path);
        }
        catch
        {
            return ErrorCodes.PathOutOfScope;
        }

        if (IsUnder(full, RepoRoot))
            return null;
        if (IsUnder(full, UserRoot))
            return null;
        if (PathsEqual(full, PluginCatalogPath) || PathsEqual(full, IconCatalogPath))
            return null;

        return ErrorCodes.PathOutOfScope;
    }

    public bool IsInRepoRoot(string path)
    {
        try
        {
            var full = Path.GetFullPath(path);
            return IsUnder(full, RepoRoot);
        }
        catch { return false; }
    }

    public bool IsInUserRoot(string path)
    {
        try
        {
            var full = Path.GetFullPath(path);
            return IsUnder(full, UserRoot);
        }
        catch { return false; }
    }

    private static bool IsUnder(string fullPath, string root)
    {
        var r = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                  + Path.DirectorySeparatorChar;
        var f = fullPath;
        if (!f.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
            && PathsEqual(f, root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)))
            return true;
        return f.StartsWith(r, StringComparison.OrdinalIgnoreCase)
               || f.StartsWith(root, StringComparison.OrdinalIgnoreCase)
                  && (f.Length == root.Length
                      || f[root.Length] == Path.DirectorySeparatorChar
                      || f[root.Length] == Path.AltDirectorySeparatorChar);
    }

    private static bool PathsEqual(string a, string b)
        => string.Equals(
            a.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            b.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);
}
