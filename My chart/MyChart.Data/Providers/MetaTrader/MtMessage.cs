namespace MyChart.Data.Providers.MetaTrader;

public abstract record MtMessage
{
    public sealed record Hello(string EaVersion, int ServerUtcOffsetMinutes) : MtMessage;
    public sealed record SymbolInfo(string Name, int Digits, string GroupHint) : MtMessage;
    public sealed record Tick(string Symbol, long ServerTimeMs, double Bid, double Ask, double Volume) : MtMessage;
    public sealed record HistoryBar(string Symbol, string Tf, long ServerTimeSec, double O, double H, double L, double C, double Vol) : MtMessage;
    public sealed record HistoryEnd(string Symbol, int Count) : MtMessage;
    public sealed record Ping : MtMessage;
    public sealed record Pong : MtMessage;
    public sealed record GetHistory(string Symbol, string Tf, long FromUtcSeconds, long ToUtcSeconds, int MaxBars) : MtMessage;
    public sealed record Unknown(string Raw) : MtMessage;
}
