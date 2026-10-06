using System.Globalization;

namespace MyChart.Data.Providers.MetaTrader;

/// <summary>
/// T5.03 MessageCodec — UTF-8 text lines, fields separated by '|'.
/// </summary>
public static class MessageCodec
{
    public static string Encode(MtMessage message) => message switch
    {
        MtMessage.Hello h => $"HELLO|{h.EaVersion}|{h.ServerUtcOffsetMinutes}",
        MtMessage.SymbolInfo s => $"SYM|{s.Name}|{s.Digits}|{s.GroupHint}",
        MtMessage.Tick t => string.Format(CultureInfo.InvariantCulture,
            "T|{0}|{1}|{2}|{3}|{4}", t.Symbol, t.ServerTimeMs, t.Bid, t.Ask, t.Volume),
        MtMessage.HistoryBar b => string.Format(CultureInfo.InvariantCulture,
            "H|{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}",
            b.Symbol, b.Tf, b.ServerTimeSec, b.O, b.H, b.L, b.C, b.Vol),
        MtMessage.HistoryEnd e => $"HEND|{e.Symbol}|{e.Count}",
        MtMessage.Ping => "PING",
        MtMessage.Pong => "PONG",
        MtMessage.GetHistory g => $"GETH|{g.Symbol}|{g.Tf}|{g.FromUtcSeconds}|{g.ToUtcSeconds}|{g.MaxBars}",
        MtMessage.Unknown u => u.Raw,
        _ => throw new ArgumentOutOfRangeException(nameof(message))
    };

    public static MtMessage Decode(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return new MtMessage.Unknown(line ?? "");

        var parts = line.Trim().Split('|');
        var head = parts[0];

        try
        {
            return head switch
            {
                "HELLO" when parts.Length >= 3 =>
                    new MtMessage.Hello(parts[1], int.Parse(parts[2], CultureInfo.InvariantCulture)),
                "SYM" when parts.Length >= 4 =>
                    new MtMessage.SymbolInfo(parts[1], int.Parse(parts[2], CultureInfo.InvariantCulture), parts[3]),
                "T" when parts.Length >= 6 =>
                    new MtMessage.Tick(
                        parts[1],
                        long.Parse(parts[2], CultureInfo.InvariantCulture),
                        double.Parse(parts[3], CultureInfo.InvariantCulture),
                        double.Parse(parts[4], CultureInfo.InvariantCulture),
                        double.Parse(parts[5], CultureInfo.InvariantCulture)),
                "H" when parts.Length >= 9 =>
                    new MtMessage.HistoryBar(
                        parts[1], parts[2],
                        long.Parse(parts[3], CultureInfo.InvariantCulture),
                        double.Parse(parts[4], CultureInfo.InvariantCulture),
                        double.Parse(parts[5], CultureInfo.InvariantCulture),
                        double.Parse(parts[6], CultureInfo.InvariantCulture),
                        double.Parse(parts[7], CultureInfo.InvariantCulture),
                        double.Parse(parts[8], CultureInfo.InvariantCulture)),
                "HEND" when parts.Length >= 3 =>
                    new MtMessage.HistoryEnd(parts[1], int.Parse(parts[2], CultureInfo.InvariantCulture)),
                "PING" => new MtMessage.Ping(),
                "PONG" => new MtMessage.Pong(),
                "GETH" when parts.Length >= 6 =>
                    new MtMessage.GetHistory(
                        parts[1], parts[2],
                        long.Parse(parts[3], CultureInfo.InvariantCulture),
                        long.Parse(parts[4], CultureInfo.InvariantCulture),
                        int.Parse(parts[5], CultureInfo.InvariantCulture)),
                _ => new MtMessage.Unknown(line)
            };
        }
        catch (FormatException)
        {
            return new MtMessage.Unknown(line);
        }
    }
}
