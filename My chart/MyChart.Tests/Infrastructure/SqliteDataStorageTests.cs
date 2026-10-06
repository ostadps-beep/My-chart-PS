using MyChart.Core.Models.Market;
using MyChart.Infrastructure.Storage;
using Xunit;

namespace MyChart.Tests.Infrastructure;

/// <summary>T5.01 VERIFY — write/read candles; tick purge.</summary>
public class SqliteDataStorageTests
{
    private static string TempDb()
    {
        var path = Path.Combine(Path.GetTempPath(), "mychart-test-" + Guid.NewGuid().ToString("N") + ".db");
        return path;
    }

    [Fact]
    public void WriteAndRead_Candles_RoundTrip()
    {
        var path = TempDb();
        try
        {
            using var db = new SqliteDataStorage(path);
            db.Open();
            db.UpsertSymbol(new SymbolInfo("EURUSD", "CSV", SymbolGroup.Forex, 5));

            var start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
            var bars = new List<Candle>();
            for (int i = 0; i < 250; i++)
            {
                bars.Add(new Candle(start.AddMinutes(i), 1.1 + i * 0.00001, 1.11, 1.09, 1.105, i + 1));
            }

            db.WriteCandles("EURUSD", Timeframe.M1, bars);
            Assert.Equal(250, db.CountCandles("EURUSD", Timeframe.M1));

            var read = db.ReadCandles("EURUSD", Timeframe.M1);
            Assert.Equal(250, read.Count);
            Assert.Equal(bars[0].Timestamp, read[0].Timestamp);
            Assert.Equal(bars[0].Open, read[0].Open, 10);
            Assert.Equal(bars[^1].Close, read[^1].Close, 10);
            Assert.Equal(bars[^1].Volume, read[^1].Volume, 10);
        }
        finally
        {
            try { File.Delete(path); } catch { /* ignore */ }
            try { File.Delete(path + "-wal"); } catch { /* ignore */ }
            try { File.Delete(path + "-shm"); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void Write_100000_Candles_ReadBack()
    {
        var path = TempDb();
        try
        {
            using var db = new SqliteDataStorage(path);
            db.Open();
            var start = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
            const int n = 100_000;
            var bars = new List<Candle>(n);
            for (int i = 0; i < n; i++)
            {
                double p = 1.1 + (i % 1000) * 0.00001;
                bars.Add(new Candle(start.AddMinutes(i), p, p + 0.0001, p - 0.0001, p, 1));
            }
            db.WriteCandles("EURUSD", Timeframe.M1, bars);
            Assert.Equal(n, db.CountCandles("EURUSD", Timeframe.M1));

            var read = db.ReadCandles("EURUSD", Timeframe.M1);
            Assert.Equal(n, read.Count);
            Assert.Equal(bars[0].Timestamp, read[0].Timestamp);
            Assert.Equal(bars[n - 1].Timestamp, read[n - 1].Timestamp);
            Assert.Equal(bars[50000].Open, read[50000].Open, 10);
        }
        finally
        {
            try { File.Delete(path); } catch { /* ignore */ }
            try { File.Delete(path + "-wal"); } catch { /* ignore */ }
            try { File.Delete(path + "-shm"); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void TickPurge_RemovesOlderThan7Days()
    {
        var path = TempDb();
        try
        {
            using var db = new SqliteDataStorage(path);
            db.Open();
            var now = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
            var ticks = new List<(DateTimeOffset, double, double, double)>
            {
                (now.AddDays(-10), 1.1, 1.1001, 1),
                (now.AddDays(-8), 1.1, 1.1001, 1),
                (now.AddDays(-3), 1.1, 1.1001, 1),
                (now.AddHours(-1), 1.1, 1.1001, 1),
            };
            db.WriteTicks("EURUSD", ticks);
            Assert.Equal(4, db.CountTicks("EURUSD"));
            int removed = db.PurgeOldTicks(now);
            Assert.Equal(2, removed);
            Assert.Equal(2, db.CountTicks("EURUSD"));
        }
        finally
        {
            try { File.Delete(path); } catch { /* ignore */ }
            try { File.Delete(path + "-wal"); } catch { /* ignore */ }
            try { File.Delete(path + "-shm"); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void Layout_RoundTrip()
    {
        var path = TempDb();
        try
        {
            using var db = new SqliteDataStorage(path);
            db.Open();
            db.SaveLayout("default", "{\"x\":1}");
            Assert.Equal("{\"x\":1}", db.LoadLayout("default"));
        }
        finally
        {
            try { File.Delete(path); } catch { /* ignore */ }
        }
    }
}
