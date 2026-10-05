namespace MyChart.Generator.Engine.Operations;

/// <summary>PG3.06 ValidateReport — read-only scan results.</summary>
public sealed class ValidateReport
{
    public List<string> MissingFiles { get; } = new();
    public List<string> OrphanFiles { get; } = new();
    public List<string> HandEditedGenerated { get; } = new();
    public List<string> BrokenReferences { get; } = new();
    public List<string> Duplicates { get; } = new();
    public bool CatalogStale { get; set; }
    public List<(string Code, string Message)> Errors { get; } = new();

    public bool Ok =>
        MissingFiles.Count == 0
        && HandEditedGenerated.Count == 0
        && BrokenReferences.Count == 0
        && Duplicates.Count == 0
        && !CatalogStale
        && Errors.Count == 0;
}
