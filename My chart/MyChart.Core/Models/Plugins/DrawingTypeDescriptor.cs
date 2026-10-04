namespace MyChart.Core.Models.Plugins;

public sealed record DrawingTypeDescriptor(
    string TypeId,
    int TypeVersion,
    int AnchorCount);
