using System.Diagnostics;
using System.Xml.Linq;
using MyChart.Core.Models.Market;
using Xunit;

namespace MyChart.Tests.Smoke;

/// <summary>
/// T7.05 ReleaseChecklist — automated parts of the release gate.
/// Owner still runs: Release x64 build; optional long stress (30 min) outside CI.
/// </summary>
public class ReleaseChecklistTests
{
    private static string RepoRoot()
    {
        // Tests run from bin/Debug/net8.0 — walk up to folder containing MyChart.sln
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "MyChart.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }
        // Fallback: relative from test output
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
    }

    private static IReadOnlyList<string> ProjectReferences(string csprojPath)
    {
        if (!File.Exists(csprojPath)) return Array.Empty<string>();
        var doc = XDocument.Load(csprojPath);
        return doc.Descendants("ProjectReference")
            .Select(e => (string?)e.Attribute("Include") ?? "")
            .Where(s => s.Length > 0)
            .Select(s => Path.GetFileNameWithoutExtension(s.Replace('\\', '/')))
            .ToList();
    }

    [Fact]
    public void ProjectGraph_Core_References_Nothing()
    {
        var root = RepoRoot();
        var refs = ProjectReferences(Path.Combine(root, "MyChart.Core", "MyChart.Core.csproj"));
        Assert.Empty(refs);
    }

    [Fact]
    public void ProjectGraph_Core_DoesNotReference_Forbidden()
    {
        var root = RepoRoot();
        var core = Path.Combine(root, "MyChart.Core", "MyChart.Core.csproj");
        var text = File.ReadAllText(core);
        Assert.DoesNotContain("MyChart.PluginHost", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MyChart.Plugins", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MyChart.Generator", text, StringComparison.Ordinal);
        Assert.DoesNotContain("MyChart.ToolBuilder", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ProjectGraph_Rendering_Only_Core()
    {
        var root = RepoRoot();
        var refs = ProjectReferences(Path.Combine(root, "MyChart.Rendering", "MyChart.Rendering.csproj"));
        Assert.All(refs, r => Assert.Equal("MyChart.Core", r));
    }

    [Fact]
    public void ProjectGraph_Settings_References_Nothing()
    {
        var root = RepoRoot();
        var path = Path.Combine(root, "MyChart.Settings", "MyChart.Settings.csproj");
        if (!File.Exists(path)) return; // optional if missing in some checkouts
        var refs = ProjectReferences(path);
        Assert.Empty(refs);
    }

    [Fact]
    public void Candle_HasNoColorFields()
    {
        var fields = typeof(Candle).GetProperties();
        var names = fields.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("Color", names);
        Assert.DoesNotContain("Brush", names);
        Assert.DoesNotContain("Theme", names);
        Assert.True(names.Contains("Open") && names.Contains("Close"));
    }

    /// <summary>
    /// Memory budget proxy: 500_000 M1 candles should stay well under 300 MB managed size for the series alone.
    /// Full process budget is owner Release run; this guards structural bloat of Candle.
    /// </summary>
    [Fact]
    public void Memory_500k_Candles_Under_300MB_Estimate()
    {
        const int n = 500_000;
        var before = GC.GetTotalMemory(forceFullCollection: true);
        var bars = new Candle[n];
        var t0 = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        for (int i = 0; i < n; i++)
            bars[i] = new Candle(t0.AddMinutes(i), 1.1, 1.11, 1.09, 1.105, 1.0);
        var after = GC.GetTotalMemory(forceFullCollection: false);
        long delta = after - before;
        // Keep a hard ceiling: 300 MB = 314_572_800
        Assert.True(delta < 300L * 1024 * 1024,
            $"500k candles grew managed memory by {delta / (1024.0 * 1024):F1} MB (limit 300 MB).");
        // Touch so array is not optimized away
        Assert.Equal(n, bars.Length);
        Assert.True(bars[^1].Timestamp > bars[0].Timestamp);
    }

    /// <summary>Short stress smoke: many ticks without unhandled exception (not the full 30 min gate).</summary>
    [Fact]
    public void StressSmoke_100k_TickLike_Updates_NoThrow()
    {
        var sw = Stopwatch.StartNew();
        double acc = 0;
        for (int i = 0; i < 100_000; i++)
        {
            var c = new Candle(
                DateTimeOffset.UtcNow,
                1.0 + (i % 100) * 0.0001,
                1.0 + (i % 100) * 0.0001 + 0.0002,
                1.0 + (i % 100) * 0.0001 - 0.0002,
                1.0 + (i % 100) * 0.0001 + 0.0001,
                i);
            acc += c.Close;
        }
        sw.Stop();
        Assert.True(acc > 0);
        Assert.True(sw.ElapsedMilliseconds < 30_000, "Smoke stress should finish under 30s");
    }
}
