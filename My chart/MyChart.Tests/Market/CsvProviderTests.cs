using MyChart.Core.Models.Market;
using MyChart.Data.Providers;
using Xunit;

namespace MyChart.Tests.Market;

/// <summary>T1.10 VERIFY — load Fixtures/EURUSD_M1.csv; wrong header → SourceCorrupted.</summary>
public class CsvProviderTests
{
    private static string FixturesDir
    {
        get
        {
            // Resolve relative to test assembly
            var dir = Path.GetDirectoryName(typeof(CsvProviderTests).Assembly.Location)!;
            var fixtures = Path.Combine(dir, "Fixtures");
            if (Directory.Exists(fixtures)) return fixtures;
            // fallback: walk up to project Fixtures
            var cur = new DirectoryInfo(dir);
            while (cur != null)
            {
                var candidate = Path.Combine(cur.FullName, "Fixtures");
                if (Directory.Exists(candidate)) return candidate;
                candidate = Path.Combine(cur.FullName, "MyChart.Tests", "Fixtures");
                if (Directory.Exists(candidate)) return candidate;
                cur = cur.Parent;
            }
            return Path.Combine(dir, "Fixtures");
        }
    }

    [Fact]
    public async Task Load_EURUSD_M1_NoException()
    {
        var provider = new CsvProvider(FixturesDir);
        await provider.ConnectAsync(CancellationToken.None);
        var candles = await provider.GetHistoryAsync("EURUSD", Timeframe.M1, null, null, 0, CancellationToken.None);
        Assert.True(candles.Count >= 1);
        Assert.Equal(TimeSpan.Zero, candles[0].Timestamp.Offset);
    }

    [Fact]
    public async Task WrongHeader_ThrowsSourceCorrupted()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "mychart_csv_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);
        try
        {
            File.WriteAllText(Path.Combine(tmp, "BAD_M1.csv"), "time,o,h,l,c,v\n2024-01-01T00:00:00Z,1,1,1,1,1\n");
            var provider = new CsvProvider(tmp);
            await provider.ConnectAsync(CancellationToken.None);
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await provider.GetHistoryAsync("BAD", Timeframe.M1, null, null, 0, CancellationToken.None));
            Assert.Contains("SourceCorrupted", ex.Message);
        }
        finally
        {
            Directory.Delete(tmp, true);
        }
    }
}
