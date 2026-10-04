namespace MyChart.Core.Models.Market;

public readonly record struct Tick(
    DateTimeOffset Timestamp,
    double Bid,
    double Ask,
    double Volume);
