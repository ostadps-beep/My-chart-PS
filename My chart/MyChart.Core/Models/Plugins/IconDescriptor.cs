namespace MyChart.Core.Models.Plugins;

public sealed record IconDescriptor(
    string IconKey,
    string PathData,
    int ViewBox = 16);
