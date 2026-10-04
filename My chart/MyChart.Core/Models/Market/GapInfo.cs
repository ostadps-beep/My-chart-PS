namespace MyChart.Core.Models.Market;

public sealed record GapInfo(DateTimeOffset From, DateTimeOffset To, GapKind Kind);
