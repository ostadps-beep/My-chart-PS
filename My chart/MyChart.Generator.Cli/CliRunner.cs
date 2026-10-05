using MyChart.Generator.Engine.Operations;

namespace MyChart.Generator.Cli;

/// <summary>PG3.08 command dispatcher for mychart-gen.</summary>
public static class CliRunner
{
    public static int Run(string[] args, TextWriter stdout, TextWriter stderr)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            WriteHelp(stderr);
            return 0;
        }

        var opt = CliOptions.Parse(args);
        var userRoot = opt.UserRoot
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MyChart", "Components");

        var scope = new PathScope(opt.Repo, userRoot);
        var tools = new ToolOperations(scope);
        var icons = new IconOperations(scope);
        var target = string.Equals(opt.Target, "user", StringComparison.OrdinalIgnoreCase)
            ? ComponentTarget.User
            : ComponentTarget.Repo;

        try
        {
            return opt.Verb.ToLowerInvariant() switch
            {
                "tool" => RunTool(opt, tools, target, stdout, stderr),
                "icon" => RunIcon(opt, icons, target, stdout, stderr),
                "list" => RunList(tools, target, opt, stdout),
                "validate" => RunValidate(scope, opt, stdout, stderr),
                "regenerate" => Emit(tools.RegenerateCatalogs(opt.DryRun, opt.AllowDirty), "regenerate", opt, stdout),
                "undo" => RunUndo(scope, opt, stdout, stderr),
                _ => Unknown(opt.Verb, stderr)
            };
        }
        catch (Exception ex)
        {
            stderr.WriteLine(ex.Message);
            if (opt.Json)
            {
                var r = new CliResult
                {
                    Ok = false,
                    Operation = opt.Verb,
                    ExitCode = 5,
                    Errors = { new CliError { Code = "E_UNEXPECTED", Message = ex.Message } }
                };
                stdout.WriteLine(JsonOutput.Serialize(r));
            }
            return 5;
        }
    }

    private static int RunTool(CliOptions opt, ToolOperations tools, ComponentTarget target, TextWriter stdout, TextWriter stderr)
    {
        if (opt.Positional.Count == 0)
            return Unknown("tool (missing subcommand)", stderr);

        var sub = opt.Positional[0].ToLowerInvariant();
        return sub switch
        {
            "add" => Emit(tools.Add(new ToolAddRequest
            {
                ComponentId = opt.Named.GetValueOrDefault("id") ?? (opt.Positional.Count > 1 ? opt.Positional[1] : ""),
                Name = opt.Named.GetValueOrDefault("name") ?? opt.Named.GetValueOrDefault("id") ?? "Tool",
                Kind = string.Equals(opt.Named.GetValueOrDefault("kind"), "CodeTool", StringComparison.OrdinalIgnoreCase)
                    ? ToolKind.CodeTool
                    : ToolKind.DataTool,
                Anchors = int.TryParse(opt.Named.GetValueOrDefault("anchors"), out var a) ? a : 2,
                IconKey = opt.Named.GetValueOrDefault("icon"),
                Hotkey = opt.Named.GetValueOrDefault("hotkey"),
                Target = target,
                DryRun = opt.DryRun,
                AllowDirty = opt.AllowDirty,
                ClickThenText = string.Equals(opt.Named.GetValueOrDefault("workflow"), "ClickThenText", StringComparison.OrdinalIgnoreCase)
            }), "tool add", opt, stdout),

            "remove" => Emit(
                tools.Remove(
                    opt.Named.GetValueOrDefault("id") ?? (opt.Positional.Count > 1 ? opt.Positional[1] : ""),
                    target,
                    dryRun: opt.DryRun || !opt.Yes,
                    allowDirty: opt.AllowDirty),
                "tool remove", opt, stdout),

            "enable" => Emit(
                tools.SetActive(opt.Named.GetValueOrDefault("id") ?? opt.Positional.ElementAtOrDefault(1) ?? "", target, true, opt.DryRun, opt.AllowDirty),
                "tool enable", opt, stdout),

            "disable" => Emit(
                tools.SetActive(opt.Named.GetValueOrDefault("id") ?? opt.Positional.ElementAtOrDefault(1) ?? "", target, false, opt.DryRun, opt.AllowDirty),
                "tool disable", opt, stdout),

            _ => Unknown("tool " + sub, stderr)
        };
    }

    private static int RunIcon(CliOptions opt, IconOperations icons, ComponentTarget target, TextWriter stdout, TextWriter stderr)
    {
        if (opt.Positional.Count == 0)
            return Unknown("icon (missing subcommand)", stderr);

        var sub = opt.Positional[0].ToLowerInvariant();
        return sub switch
        {
            "add" => Emit(icons.Add(new IconAddRequest
            {
                ComponentId = opt.Named.GetValueOrDefault("id") ?? (opt.Positional.Count > 1 ? opt.Positional[1] : ""),
                Name = opt.Named.GetValueOrDefault("name") ?? "Icon",
                IconKey = opt.Named.GetValueOrDefault("key") ?? ("Icon." + (opt.Named.GetValueOrDefault("id") ?? "X")),
                GeometryPathData = opt.Named.GetValueOrDefault("geometry") ?? "M2 2 L14 14",
                Target = target,
                DryRun = opt.DryRun,
                AllowDirty = opt.AllowDirty
            }), "icon add", opt, stdout),

            "remove" => Emit(
                icons.Remove(
                    opt.Named.GetValueOrDefault("id") ?? (opt.Positional.Count > 1 ? opt.Positional[1] : ""),
                    target,
                    dryRun: opt.DryRun || !opt.Yes,
                    allowDirty: opt.AllowDirty),
                "icon remove", opt, stdout),

            _ => Unknown("icon " + sub, stderr)
        };
    }

    private static int RunList(ToolOperations tools, ComponentTarget target, CliOptions opt, TextWriter stdout)
    {
        var list = tools.List(target);
        if (opt.Json)
        {
            var r = new CliResult { Ok = true, Operation = "list", ExitCode = 0 };
            foreach (var m in list)
                r.Warnings.Add(m.ComponentId + " " + m.Kind + " active=" + m.Active);
            stdout.WriteLine(JsonOutput.Serialize(r));
        }
        else
        {
            foreach (var m in list)
                stdout.WriteLine(m.ComponentId + "\t" + m.Kind + "\t" + (m.Active ? "active" : "disabled"));
        }
        return 0;
    }

    private static int RunValidate(PathScope scope, CliOptions opt, TextWriter stdout, TextWriter stderr)
    {
        var report = new ValidateReport { };
        // lightweight: scan both roots for manifests
        var components = new List<Validate.ComponentRecord>();
        void Scan(string root)
        {
            if (!Directory.Exists(root)) return;
            foreach (var dir in Directory.EnumerateDirectories(root))
            {
                var mp = Path.Combine(dir, "Manifest.json");
                if (!File.Exists(mp)) continue;
                try
                {
                    var m = ManifestIO.ReadFile(mp);
                    components.Add(new Validate.ComponentRecord
                    {
                        ComponentId = m.ComponentId,
                        Kind = m.Kind,
                        RootPath = root,
                        ManifestPath = mp,
                        Manifest = m,
                        DefinitionPath = Path.Combine(dir, "Definition.json"),
                        GeometryPath = Path.Combine(dir, "Geometry.txt")
                    });
                }
                catch { /* skip */ }
            }
        }

        Scan(Path.Combine(scope.RepoRoot, "MyChart.Plugins", "Components"));
        Scan(scope.UserRoot);

        report = Validate.Run(components);
        var r = new CliResult
        {
            Ok = report.Ok,
            Operation = "validate",
            ExitCode = report.Ok ? 0 : 1
        };
        foreach (var e in report.Errors)
            r.Errors.Add(new CliError { Code = e.Code, Message = e.Message });
        foreach (var f in report.MissingFiles)
            r.Errors.Add(new CliError { Code = "E_MISSING", Message = f, Path = f });

        if (opt.Json)
            stdout.WriteLine(JsonOutput.Serialize(r));
        else if (!report.Ok)
            stderr.WriteLine("validate failed: " + r.Errors.Count + " issue(s)");

        return r.ExitCode;
    }

    private static int RunUndo(PathScope scope, CliOptions opt, TextWriter stdout, TextWriter stderr)
    {
        var git = new GitGateway(scope.RepoRoot);
        if (!git.IsGitAvailable())
        {
            var r = new CliResult
            {
                Ok = false,
                Operation = "undo",
                ExitCode = 5,
                Errors = { new CliError { Code = ErrorCodes.GitUnavailable, Message = "git is not available" } }
            };
            if (opt.Json) stdout.WriteLine(JsonOutput.Serialize(r));
            else stderr.WriteLine(ErrorCodes.GitUnavailable);
            return 5;
        }

        var exit = git.UndoLastGenCommit();
        var result = new CliResult
        {
            Ok = exit == 0,
            Operation = "undo",
            ExitCode = exit == 0 ? 0 : 1
        };
        if (opt.Json) stdout.WriteLine(JsonOutput.Serialize(result));
        return result.ExitCode;
    }

    private static int Emit(TransactionResult tx, string operation, CliOptions opt, TextWriter stdout)
    {
        var r = JsonOutput.FromTransaction(operation, tx);
        if (opt.Json)
            stdout.WriteLine(JsonOutput.Serialize(r));
        else if (!tx.Ok)
            stdout.WriteLine((tx.ErrorCode ?? "error") + ": " + (tx.ErrorMessage ?? ""));
        return r.ExitCode;
    }

    private static int Unknown(string what, TextWriter stderr)
    {
        stderr.WriteLine("Unknown command: " + what);
        WriteHelp(stderr);
        return 1;
    }

    private static void WriteHelp(TextWriter w)
    {
        w.WriteLine("mychart-gen — MyChart component generator");
        w.WriteLine("Verbs:");
        w.WriteLine("  tool add --id <Id> [--name <Name>] [--kind DataTool|CodeTool] [--anchors N] [--icon <key>] [--target repo|user]");
        w.WriteLine("  tool remove --id <Id> --yes");
        w.WriteLine("  tool enable|disable --id <Id>");
        w.WriteLine("  icon add --id <Id> --key <IconKey> [--geometry \"M..\"]");
        w.WriteLine("  icon remove --id <Id> --yes");
        w.WriteLine("  list | validate | regenerate | undo");
        w.WriteLine("Global: --dry-run --json --yes --commit --allow-dirty --build --repo <path> --target repo|user --user-root <path>");
    }
}
