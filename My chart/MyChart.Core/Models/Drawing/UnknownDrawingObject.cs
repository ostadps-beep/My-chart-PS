using System.Text.Json.Nodes;

namespace MyChart.Core.Models.Drawing;

/// <summary>
/// Placeholder for drawings whose TypeId is no longer registered.
/// </summary>
public sealed record UnknownDrawingObject(
    string Id,
    string TypeId,
    int TypeVersion,
    JsonObject? Extra);
