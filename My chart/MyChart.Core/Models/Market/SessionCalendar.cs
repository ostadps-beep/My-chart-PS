namespace MyChart.Core.Models.Market;

/// <summary>
/// T1.02 — Session calendar binding a time rule to a symbol group.
/// </summary>
public sealed record SessionCalendar(ServerTimeRule Rule, SymbolGroup Group);
