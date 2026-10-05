namespace MyChart.Generator.Cli;

public sealed class CliOptions
{
    public bool DryRun { get; set; }
    public bool Json { get; set; }
    public bool Yes { get; set; }
    public bool Commit { get; set; }
    public bool AllowDirty { get; set; }
    public bool Build { get; set; }
    public string Repo { get; set; } = Directory.GetCurrentDirectory();
    public string Target { get; set; } = "repo"; // repo|user
    public string? UserRoot { get; set; }
    public string Verb { get; set; } = "";
    public List<string> Positional { get; set; } = new();
    public Dictionary<string, string> Named { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static CliOptions Parse(string[] args)
    {
        var o = new CliOptions();
        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];
            switch (a)
            {
                case "--dry-run": o.DryRun = true; break;
                case "--json": o.Json = true; break;
                case "--yes": o.Yes = true; break;
                case "--commit": o.Commit = true; break;
                case "--allow-dirty": o.AllowDirty = true; break;
                case "--build": o.Build = true; break;
                case "--repo":
                    if (i + 1 < args.Length) o.Repo = Path.GetFullPath(args[++i]);
                    break;
                case "--target":
                    if (i + 1 < args.Length) o.Target = args[++i];
                    break;
                case "--user-root":
                    if (i + 1 < args.Length) o.UserRoot = Path.GetFullPath(args[++i]);
                    break;
                default:
                    if (a.StartsWith("--") && i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                    {
                        o.Named[a[2..]] = args[++i];
                    }
                    else if (!a.StartsWith("-"))
                    {
                        if (string.IsNullOrEmpty(o.Verb))
                            o.Verb = a;
                        else
                            o.Positional.Add(a);
                    }
                    break;
            }
        }
        return o;
    }
}
