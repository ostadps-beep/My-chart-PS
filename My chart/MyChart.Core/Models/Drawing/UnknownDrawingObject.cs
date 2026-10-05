using System.Text.Json.Nodes;

namespace MyChart.Core.Models.Drawing;

/// <summary>
/// PG1.05 / T3.07: unknown typeId or newer typeVersion. Not painted, not hit, not selectable.
/// Written back byte-identical via RawJson.
/// </summary>
public sealed record UnknownDrawingObject(
    string Id,
    string TypeId,
    int TypeVersion,
    JsonObject? Extra,
    JsonObject? RawJson = null);
