using MyChart.Core.Models.Rendering;

namespace MyChart.Core.Contracts.Rendering;

/// <summary>
/// Draws a single render layer onto an <see cref="IRenderContext"/>.
/// Implementations must not perform IO, DB or network access.
/// </summary>
public interface ILayerRenderer
{
    RenderLayer Layer { get; }

    /// <summary>Paint this layer. Coordinates are device pixels.</summary>
    void Render(IRenderContext context);
}
