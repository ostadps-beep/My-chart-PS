using System.Diagnostics;
using MyChart.Core.Plugins.Manifest;

namespace MyChart.Generator.Engine.Operations;

/// <summary>
/// PG3.04 GitGateway — clean-tree check and optional commit/revert for RepoRoot ops.
/// </summary>
public sealed class GitGateway
{
    private readonly string _repoRoot;

    public GitGateway(string repoRoot)
    {
        _repoRoot = Path.GetFullPath(repoRoot);
    }

    /// <summary>
    /// Returns null if git is available and (allowDirty or working tree clean).
    /// E025 if git missing; E024 if dirty and not allowDirty.
    /// </summary>
    public string? EnsureCleanOrAllowed(bool allowDirty)
    {
        if (!IsGitAvailable())
            return ErrorCodes.GitUnavailable;

        if (allowDirty)
            return null;

        if (!IsWorkingTreeClean())
            return ErrorCodes.GitNotClean;

        return null;
    }

    public bool IsGitAvailable()
    {
        try
        {
            var (exit, _, _) = Run("rev-parse", "--is-inside-work-tree");
            return exit == 0;
        }
        catch
        {
            return false;
        }
    }

    public bool IsWorkingTreeClean()
    {
        var (exit, stdout, _) = Run("status", "--porcelain");
        if (exit != 0) return false;
        return string.IsNullOrWhiteSpace(stdout);
    }

    /// <summary>Commit with message prefix "gen:" as required for undo.</summary>
    public int Commit(string messageBody)
    {
        var msg = messageBody.StartsWith("gen:", StringComparison.Ordinal)
            ? messageBody
            : "gen: " + messageBody;
        Run("add", "-A");
        var (exit, _, _) = Run("commit", "-m", msg);
        return exit;
    }

    /// <summary>Revert last commit whose message starts with "gen:".</summary>
    public int UndoLastGenCommit()
    {
        var (exit, stdout, _) = Run("log", "-1", "--pretty=%s");
        if (exit != 0) return exit;
        var subject = stdout.Trim();
        if (!subject.StartsWith("gen:", StringComparison.Ordinal))
            return 1;
        var (exit2, _, _) = Run("revert", "--no-edit", "HEAD");
        return exit2;
    }

    private (int Exit, string StdOut, string StdErr) Run(params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = _repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var a in args)
            psi.ArgumentList.Add(a);

        using var p = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start git");
        var stdout = p.StandardOutput.ReadToEnd();
        var stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();
        return (p.ExitCode, stdout, stderr);
    }
}
