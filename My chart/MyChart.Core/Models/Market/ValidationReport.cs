namespace MyChart.Core.Models.Market;

/// <summary>
/// T1.03 ValidationReport.
/// </summary>
public sealed record ValidationReport(
    int Accepted,
    int Repaired,
    int Rejected,
    int Duplicates,
    int Corrupted,
    IReadOnlyList<GapInfo> Gaps);
