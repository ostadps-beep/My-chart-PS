using MyChart.Core.Plugins.Manifest;

namespace MyChart.Generator.Engine.Operations;

/// <summary>
/// PG3.04 Transaction — PLAN → VALIDATE → GIT CHECK → WRITE .tmp → VERIFY → COMMIT (rename).
/// Dry-run stops after VALIDATE. Failure before COMMIT deletes temps and leaves tree unchanged.
/// </summary>
public sealed class Transaction
{
    private readonly PathScope _scope;
    private readonly GitGateway? _git;

    public Transaction(PathScope scope, GitGateway? git = null)
    {
        _scope = scope;
        _git = git;
    }

    public TransactionResult Execute(
        FilePlan plan,
        bool dryRun = false,
        bool allowDirty = false,
        bool requireGitForRepoPaths = true,
        Func<FilePlan, string?>? verify = null,
        Action<int, FilePlanEntry>? onBeforeWrite = null)
    {
        var result = new TransactionResult { Plan = plan, DryRun = dryRun };

        // VALIDATE PathScope + E014 (user file would be overwritten on Create when exists)
        foreach (var e in plan.Entries)
        {
            var scopeErr = _scope.Validate(e.Path);
            if (scopeErr is not null)
            {
                result.ErrorCode = scopeErr;
                result.ErrorMessage = $"Path out of scope: {e.Path}";
                result.ExitCode = 4;
                return result;
            }

            if (e.Op == FileOpKind.Create && File.Exists(e.Path))
            {
                // Creating over existing user file
                if (_scope.IsInUserRoot(e.Path))
                {
                    result.ErrorCode = ErrorCodes.UserFileWouldBeOverwritten;
                    result.ErrorMessage = e.Path;
                    result.ExitCode = 1;
                    return result;
                }
            }
        }

        // GIT CHECK for any RepoRoot path
        bool touchesRepo = plan.Entries.Any(e => _scope.IsInRepoRoot(e.Path));
        if (touchesRepo && requireGitForRepoPaths)
        {
            var git = _git ?? new GitGateway(_scope.RepoRoot);
            var gitErr = git.EnsureCleanOrAllowed(allowDirty);
            if (gitErr is not null)
            {
                result.ErrorCode = gitErr;
                result.ErrorMessage = gitErr == ErrorCodes.GitUnavailable
                    ? "git is not available"
                    : "working tree is not clean";
                result.ExitCode = gitErr == ErrorCodes.GitUnavailable ? 5 : 1;
                return result;
            }
        }

        if (dryRun)
        {
            result.Ok = true;
            result.ExitCode = 0;
            return result;
        }

        // WRITE to .tmp then COMMIT (rename); deletes last
        var temps = new List<(string Temp, string Final, FilePlanEntry Entry)>();
        try
        {
            int index = 0;
            foreach (var e in plan.Entries.Where(x => x.Op != FileOpKind.Delete))
            {
                onBeforeWrite?.Invoke(index, e);
                index++;

                var dir = Path.GetDirectoryName(e.Path);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                var temp = e.Path + ".tmp";
                File.WriteAllText(temp, e.Content ?? "");
                temps.Add((temp, e.Path, e));
                result.WrittenTempPaths.Add(temp);
            }

            // optional VERIFY hook (manifests / catalogs / build)
            if (verify is not null)
            {
                var vErr = verify(plan);
                if (vErr is not null)
                {
                    CleanupTemps(temps.Select(t => t.Temp));
                    result.ErrorCode = vErr;
                    result.ErrorMessage = "verify failed";
                    result.ExitCode = 3;
                    return result;
                }
            }

            // COMMIT: atomic replace (delete existing then move), then deletions
            foreach (var (temp, final, _) in temps)
            {
                if (File.Exists(final))
                    File.Delete(final);
                File.Move(temp, final);
            }

            foreach (var e in plan.Entries.Where(x => x.Op == FileOpKind.Delete))
            {
                if (File.Exists(e.Path))
                    File.Delete(e.Path);
            }

            result.Ok = true;
            result.ExitCode = 0;
            result.WrittenTempPaths.Clear();
            return result;
        }
        catch (Exception ex)
        {
            CleanupTemps(temps.Select(t => t.Temp));
            // also remove any .tmp left in result list
            CleanupTemps(result.WrittenTempPaths);
            result.WrittenTempPaths.Clear();
            result.ErrorCode = "E_ROLLBACK";
            result.ErrorMessage = ex.Message;
            result.ExitCode = 3;
            return result;
        }
    }

    private static void CleanupTemps(IEnumerable<string> paths)
    {
        foreach (var t in paths)
        {
            try
            {
                if (File.Exists(t)) File.Delete(t);
            }
            catch { /* best effort */ }
        }
    }
}
