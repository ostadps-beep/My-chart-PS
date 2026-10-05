namespace MyChart.Generator.Engine;

/// <summary>T3.10 Generator engine entry (operations live under Operations/).</summary>
public sealed class GeneratorEngine
{
    public string RepoRoot { get; }
    public string UserRoot { get; }

    public GeneratorEngine(string repoRoot, string userRoot)
    {
        RepoRoot = repoRoot;
        UserRoot = userRoot;
    }
}
