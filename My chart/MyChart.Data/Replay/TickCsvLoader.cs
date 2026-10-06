using System.Globalization;
using MyChart.Core.Models.Market;

namespace MyChart.Data.Replay;

/// <summary>
/// Loads ticks from CSV: header timestamp_utc,bid,ask,volume (ISO-8601 UTC).
/// </summary>
public static class TickCsvLoader
{
    public static IReadOnlyList<Tick> Load(string path)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length == 0)
            throw new InvalidOperationException("SourceCorrupted: empty tick file");

        var header = lines[0].Trim().ToLowerInvariant();
        if (header != "timestamp_utc,bid,ask,volume")
            throw new InvalidOperationException("SourceCorrupted: wrong tick header");

        var list = new List<Tick>();
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.Length == 0) continue;
            var parts = line.Split(',');
            if (parts.Length < 4)
                throw new InvalidOperationException($"SourceCorrupted: tick row {i + 1}");

            var ts = DateTimeOffset.Parse(parts[0], CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
            var bid = double.Parse(parts[1], CultureInfo.InvariantCulture);
            var ask = double.Parse(parts[2], CultureInfo.InvariantCulture);
            var vol = double.Parse(parts[3], CultureInfo.InvariantCulture);
            list.Add(new Tick(ts, bid, ask, vol));
        }

        return list;
    }
}
