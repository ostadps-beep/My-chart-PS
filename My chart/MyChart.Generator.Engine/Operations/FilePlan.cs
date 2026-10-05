namespace MyChart.Generator.Engine.Operations;

/// <summary>PG3.04 FilePlan entry: create|overwrite|delete with path and content hashes.</summary>
public sealed class FilePlanEntry
{
    public required FileOpKind Op { get; init; }
    public required string Path { get; init; }
    public string? BeforeSha { get; init; }
    public string? AfterSha { get; init; }
    /// <summary>New content for Create/Overwrite; null for Delete.</summary>
    public string? Content { get; init; }
}

public sealed class FilePlan
{
    public List<FilePlanEntry> Entries { get; } = new();

    public void AddCreate(string path, string content)
    {
        Entries.Add(new FilePlanEntry
        {
            Op = FileOpKind.Create,
            Path = path,
            AfterSha = ManifestIO.Sha256Normalized(content),
            Content = content
        });
    }

    public void AddOverwrite(string path, string content, string? beforeSha = null)
    {
        Entries.Add(new FilePlanEntry
        {
            Op = FileOpKind.Overwrite,
            Path = path,
            BeforeSha = beforeSha,
            AfterSha = ManifestIO.Sha256Normalized(content),
            Content = content
        });
    }

    public void AddDelete(string path, string? beforeSha = null)
    {
        Entries.Add(new FilePlanEntry
        {
            Op = FileOpKind.Delete,
            Path = path,
            BeforeSha = beforeSha
        });
    }
}
