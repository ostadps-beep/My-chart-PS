using System.Text.Json.Nodes;

namespace MyChart.Core.Contracts.Plugins;

public interface IDrawingObjectMigrator
{
    JsonObject Migrate(JsonObject extra, int fromVersion);
}
