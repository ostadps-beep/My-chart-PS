using MyChart.Core.Models.Rendering;
using MyChart.Rendering.Skia;
using MyChart.Tests.Support;
using Xunit;

namespace MyChart.Tests.Rendering;

public class VisualProofTests
{
    [Fact]
    public void Tiny_scene_rendered_twice_produces_byte_identical_pngs()
    {
        var a = VisualProof.Render("T4.01", "tiny-a", 64, 48, VisualProof.PaintTinyScene, writeToDisk: false);
        var b = VisualProof.Render("T4.01", "tiny-b", 64, 48, VisualProof.PaintTinyScene, writeToDisk: false);

        Assert.NotEmpty(a);
        Assert.Equal(a.Length, b.Length);
        Assert.True(a.AsSpan().SequenceEqual(b), "PNG bytes must be identical on rerun.");
    }

    [Fact]
    public void Tiny_scene_has_png_signature()
    {
        var png = VisualProof.Render("T4.01", "signature", 32, 32, VisualProof.PaintTinyScene, writeToDisk: false);
        // PNG magic: 89 50 4E 47 0D 0A 1A 0A
        Assert.True(png.Length > 8);
        Assert.Equal(0x89, png[0]);
        Assert.Equal((byte)'P', png[1]);
        Assert.Equal((byte)'N', png[2]);
        Assert.Equal((byte)'G', png[3]);
    }

    [Fact]
    public void Skia_MeasureText_returns_positive_size()
    {
        using var ctx = new SkiaPngRenderContext(100, 50, dpiScale: 1.0);
        var size = ctx.MeasureText("Hello", new TextStyle("Segoe UI", 12));
        Assert.True(size.Width > 0);
        Assert.True(size.Height > 0);
    }

    [Fact]
    public void Recording_MeasureText_returns_positive_size()
    {
        var ctx = new Plugins.RecordingRenderContext();
        var size = ctx.MeasureText("Hello", new TextStyle("Segoe UI", 12));
        Assert.True(size.Width > 0);
        Assert.True(size.Height > 0);
    }

    [Fact]
    public void SkiaPng_clears_and_draws_without_throwing()
    {
        using var ctx = new SkiaPngRenderContext(80, 60);
        ctx.Clear(RgbaColor.FromRgb(10, 10, 10));
        ctx.FillRect(0, 0, 20, 20, RgbaColor.FromRgb(255, 0, 0));
        ctx.DrawLine(0, 0, 80, 60, RgbaColor.FromRgb(0, 255, 0), 2, null);
        var png = ctx.ToPng();
        Assert.NotEmpty(png);
    }

    [Fact]
    public void VisualProof_writeToDisk_creates_file_under_artifacts()
    {
        var png = VisualProof.Render("T4.01", "disk-check", 40, 30, VisualProof.PaintTinyScene, writeToDisk: true);
        var path = Path.Combine(VisualProof.ArtifactsRoot, "T4.01", "disk-check.png");
        Assert.True(File.Exists(path), $"Expected file at {path}");
        var fromDisk = File.ReadAllBytes(path);
        Assert.True(png.AsSpan().SequenceEqual(fromDisk));
    }
}
