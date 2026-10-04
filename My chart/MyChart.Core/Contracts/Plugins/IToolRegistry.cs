using MyChart.Core.Models.Plugins;

namespace MyChart.Core.Contracts.Plugins;

public interface IToolRegistry
{
    void Register(ToolDescriptor descriptor, Func<IToolContext, IDrawingTool> factory);
}
