namespace MyChart.Generator.Engine.Operations;

public sealed class TransactionResult
{
    public bool Ok { get; set; }
    public bool DryRun { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    /// <summary>3 = rolled back (exception before commit).</summary>
    public int ExitCode { get; set; }
    public FilePlan Plan { get; set; } = new();
    public List<string> WrittenTempPaths { get; } = new();
}
