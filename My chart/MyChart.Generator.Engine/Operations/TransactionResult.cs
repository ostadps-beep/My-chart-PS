namespace MyChart.Generator.Engine.Operations;

public sealed class TransactionResult
{
    public bool Ok { get; init; }
    public bool DryRun { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    /// <summary>3 = rolled back (exception before commit).</summary>
    public int ExitCode { get; init; }
    public FilePlan Plan { get; init; } = new();
    public List<string> WrittenTempPaths { get; } = new();
}
