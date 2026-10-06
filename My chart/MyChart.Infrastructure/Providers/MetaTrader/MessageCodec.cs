using System.Globalization;
using MyChart.Core.Models.Market;

namespace MyChart.Infrastructure.Providers.MetaTrader;

/// <summary>
/// T5.03 — UTF-8 text lines, fields separated by '|', one message per line.
/// Pure encode/decode; no IO.
/// </summary>
public static class MessageCodec
{
    public const string Ping = "PING";
    public const string Pong = "PONG";

    public static string EncodePing() => Ping;
    public static string EncodePong() => Pong;

    public static string EncodeGetHistory(string symbol, DateTimeOffset fromUtc, DateTimeOffset toUtc, int maxBars)
    {
        var fromSec = fromUtc.ToUnixTimeSeconds();
        var toSec = toUtc.ToUnixTimeSeconds();
        return string.Create(CultureInfo.InvariantCulture,
            $"GETH|{symbol}|M1|{fromSec}|{toSec}|{maxBars}");
    }

    public static bool TryParse(string line, out MtMessage message)
    {
        message = default!;
        if (string.IsNullOrWhiteSpace(line))
            return false;

        line = line.TrimEnd('\r');
        if (line.Equals(Ping, StringComparison.Ordinal))
        {
            message = new MtMessage.PingMsg();
            return true;
        }
        if (line.Equals(Pong, StringComparison.Ordinal))
        {
            message = new MtMessage.PongMsg();
            return true;
        }

        var parts = line.Split('|');
        if (parts.Length == 0)
            return false;

        switch (parts[0])
        {
            case "HELLO" when parts.Length >= 3
                && int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var offsetMin):
                message = new MtMessage.Hello(parts[1], offsetMin);
                return true;

            case "SYM" when parts.Length >= 4
                && int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var digits):
                message = new MtMessage.SymbolMsg(parts[1], digits, parts[3]);
                return true;

            case "T" when parts.Length >= 6
                && long.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var tsMs)
                && double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var bid)
                && double.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var ask)
                && double.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out var vol):
                message = new MtMessage.TickMsg(parts[1], tsMs, bid, ask, vol);
                return true;

            case "H" when parts.Length >= 8
                && long.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var barSec)
                && double.TryParse(parts[4], NumberStyles.Float, CultureInfo.InvariantCulture, out var o)
                && double.TryParse(parts[5], NumberStyles.Float, CultureInfo.InvariantCulture, out var h)
                && double.TryParse(parts[6], NumberStyles.Float, CultureInfo.InvariantCulture, out var l)
                && double.TryParse(parts[7], NumberStyles.Float, CultureInfo.InvariantCulture, out var c):
                var v = parts.Length >= 9
                    && double.TryParse(parts[8], NumberStyles.Float, CultureInfo.InvariantCulture, out var vv)
                    ? vv : 0.0;
                message = new MtMessage.HistoryBar(parts[1], parts[2], barSec, o, h, l, c, v);
                return true;

            case "HEND" when parts.Length >= 3
                && int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var count):
                message = new MtMessage.HistoryEnd(parts[1], count);
                return true;

            case "GETH" when parts.Length >= 6
                && long.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out var fromSec)
                && long.TryParse(parts[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out var toSec)
                && int.TryParse(parts[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out var maxBars):
                message = new MtMessage.GetHistory(parts[1], parts[2], fromSec, toSec, maxBars);
                return true;

            default:
                return false;
        }
    }

    public static string EncodeHello(string eaVersion, int serverUtcOffsetMinutes) =>
        string.Create(CultureInfo.InvariantCulture, $"HELLO|{eaVersion}|{serverUtcOffsetMinutes}");

    public static string EncodeSymbol(string name, int digits, string groupHint) =>
        string.Create(CultureInfo.InvariantCulture, $"SYM|{name}|{digits}|{groupHint}");

    public static string EncodeTick(string symbol, long serverTimeMs, double bid, double ask, double volume) =>
        string.Create(CultureInfo.InvariantCulture,
            $"T|{symbol}|{serverTimeMs}|{bid.ToString(CultureInfo.InvariantCulture)}|{ask.ToString(CultureInfo.InvariantCulture)}|{volume.ToString(CultureInfo.InvariantCulture)}");

    public static string EncodeHistoryBar(string symbol, long serverTimeSec, double o, double h, double l, double c, double vol) =>
        string.Create(CultureInfo.InvariantCulture,
            $"H|{symbol}|M1|{serverTimeSec}|{o.ToString(CultureInfo.InvariantCulture)}|{h.ToString(CultureInfo.InvariantCulture)}|{l.ToString(CultureInfo.InvariantCulture)}|{c.ToString(CultureInfo.InvariantCulture)}|{vol.ToString(CultureInfo.InvariantCulture)}");

    public static string EncodeHistoryEnd(string symbol, int count) =>
        string.Create(CultureInfo.InvariantCulture, $"HEND|{symbol}|{count}");

    public static SymbolGroup ParseGroupHint(string groupHint)
    {
        if (Enum.TryParse<SymbolGroup>(groupHint, ignoreCase: true, out var g))
            return g;
        return SymbolGroup.Forex;
    }

    public static DateTimeOffset ServerMsToUtc(long serverTimeMs, TimeSpan offset) =>
        DateTimeOffset.FromUnixTimeMilliseconds(serverTimeMs) - offset;

    public static DateTimeOffset ServerSecToUtc(long serverTimeSec, TimeSpan offset) =>
        DateTimeOffset.FromUnixTimeSeconds(serverTimeSec) - offset;
}

public abstract record MtMessage
{
    public sealed record Hello(string EaVersion, int ServerUtcOffsetMinutes) : MtMessage;
    public sealed record SymbolMsg(string Name, int Digits, string GroupHint) : MtMessage;
    public sealed record TickMsg(string Symbol, long ServerTimeMs, double Bid, double Ask, double Volume) : MtMessage;
    public sealed record HistoryBar(string Symbol, string Tf, long ServerTimeSec, double O, double H, double L, double C, double Vol) : MtMessage;
    public sealed record HistoryEnd(string Symbol, int Count) : MtMessage;
    public sealed record GetHistory(string Symbol, string Tf, long FromUtcSeconds, long ToUtcSeconds, int MaxBars) : MtMessage;
    public sealed record PingMsg : MtMessage;
    public sealed record PongMsg : MtMessage;
}
