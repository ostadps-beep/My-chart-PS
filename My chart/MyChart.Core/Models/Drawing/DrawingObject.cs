using System.Text.Json.Nodes;
using MyChart.Core.Models.Geometry;

namespace MyChart.Core.Models.Drawing;

/// <summary>
/// Single immutable drawing object. Tool-specific data lives in Extra.
/// </summary>
public sealed record DrawingObject(
    string Id,
    string TypeId,
    int TypeVersion,
    IReadOnlyList<DrawingAnchor> Anchors,
    DrawingStyle Style,
    bool Locked,
    bool Hidden,
    JsonObject? Extra);
