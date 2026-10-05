using MyChart.Core.Contracts.Rendering;
using MyChart.Core.Models.Rendering;
using MyChart.Rendering.Skia;

namespace MyChart.Tests.Support;

/// <summary>
/// Deterministic headless scene renderer for GATE=VISUAL nodes.
/// Writes PNG files under artifacts/visual/&lt;NodeId&gt;/ (gitignored).
/// The same scene rendered twice must produce byte-identical PNGs.
/// </summary>
public static class VisualProof
{
    public static string ArtifactsRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "artifacts", "visual"));

    /// <summary>
    /// Render a scene into an in-memory PNG and optionally write it under artifacts/visual/&lt;nodeId&gt;/.
    /// </summary>
    public static byte[] Render(
        string nodeId,
        string sceneName,
        int width,
        int height,
        Action<IRenderContext> paint,
        double dpiScale = 1.0,
        bool writeToDisk = true,
        bool antiAlias = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sceneName);
        ArgumentNullException.ThrowIfNull(paint);

        using var ctx = new SkiaPngRenderContext(width, height, dpiScale, antiAlias);
        paint(ctx);
        var png = ctx.ToPng();

        if (writeToDisk)
        {
            var dir = Path.Combine(ArtifactsRoot, nodeId);
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, sceneName + ".png");
            File.WriteAllBytes(path, png);
        }

        return png;
    }

    /// <summary>Tiny fixed scene used by T4.01 VERIFY (background + one rect + one line).</summary>
    public static void PaintTinyScene(IRenderContext ctx)
    {
        ctx.Clear(RgbaColor.FromRgb(20, 20, 24));
        ctx.FillRect(10, 10, 40, 30, RgbaColor.FromRgb(0, 180, 80));
        ctx.DrawLine(5, 5, ctx.Width - 5, ctx.Height - 5, RgbaColor.FromRgb(200, 200, 200), 1, null);
    }
}
