using System.Globalization;
using Microsoft.Data.Sqlite;
using MyChart.Core.Models.Market;

namespace MyChart.Infrastructure.Storage;

/// <summary>
/// T5.01 SqliteStorage — WAL mode; Data/mychart.db schema.
/// Batch writes: 5000 rows per transaction. Tick purge older than 7 days at open.
/// </summary>
public sealed class SqliteDataStorage : IDisposable
{
    public const int BatchRows = 5000;
    public const int TickRetentionDays = 7;

    private readonly string _connectionString;
    private SqliteConnection? _conn;

    public SqliteDataStorage(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        var dir = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();
    }

    public void Open()
    {
        if (_conn is not null) return;
        _conn = new SqliteConnection(_connectionString);
        _conn.Open();
        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = "PRAGMA journal_mode=WAL;";
            cmd.ExecuteNonQuery();
        }
        EnsureSchema();
        PurgeOldTicks();
    }

    private SqliteConnection Conn =>
        _conn ?? throw new InvalidOperationException("Storage not open. Call Open() first.");

    private void EnsureSchema()
    {
        using var cmd = Conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS Symbols (
              symbol TEXT PRIMARY KEY,
              provider_name TEXT NOT NULL,
              grp TEXT NOT NULL,
              digits INTEGER NOT NULL
            );
            CREATE TABLE IF NOT EXISTS Candles (
              symbol TEXT NOT NULL,
              tf TEXT NOT NULL,
              ts_ms INTEGER NOT NULL,
              o REAL NOT NULL,
              h REAL NOT NULL,
              l REAL NOT NULL,
              c REAL NOT NULL,
              v REAL NOT NULL,
              PRIMARY KEY (symbol, tf, ts_ms)
            );
            CREATE TABLE IF NOT EXISTS Ticks (
              symbol TEXT NOT NULL,
              ts_ms INTEGER NOT NULL,
              bid REAL NOT NULL,
              ask REAL NOT NULL,
              vol REAL NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_Ticks_Symbol_Ts ON Ticks(symbol, ts_ms);
            CREATE TABLE IF NOT EXISTS IndicatorsCache (
              key TEXT PRIMARY KEY,
              payload BLOB NOT NULL,
              updated_ms INTEGER NOT NULL
            );
            CREATE TABLE IF NOT EXISTS Layouts (
              name TEXT PRIMARY KEY,
              json TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    public void UpsertSymbol(SymbolInfo symbol)
    {
        using var cmd = Conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Symbols(symbol, provider_name, grp, digits)
            VALUES ($s, $p, $g, $d)
            ON CONFLICT(symbol) DO UPDATE SET
              provider_name = excluded.provider_name,
              grp = excluded.grp,
              digits = excluded.digits;
            """;
        cmd.Parameters.AddWithValue("$s", symbol.Name);
        cmd.Parameters.AddWithValue("$p", symbol.ProviderName);
        cmd.Parameters.AddWithValue("$g", symbol.Group.ToString());
        cmd.Parameters.AddWithValue("$d", symbol.Digits);
        cmd.ExecuteNonQuery();
    }

    public void WriteCandles(string symbol, Timeframe tf, IReadOnlyList<Candle> candles)
    {
        if (candles.Count == 0) return;
        // Only M1 plus native D1,W1,MN1 are stored
        if (tf is not (Timeframe.M1 or Timeframe.D1 or Timeframe.W1 or Timeframe.MN1))
            return;

        for (int offset = 0; offset < candles.Count; offset += BatchRows)
        {
            int count = Math.Min(BatchRows, candles.Count - offset);
            using var tx = Conn.BeginTransaction();
            using var cmd = Conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO Candles(symbol, tf, ts_ms, o, h, l, c, v)
                VALUES ($s, $tf, $ts, $o, $h, $l, $c, $v)
                ON CONFLICT(symbol, tf, ts_ms) DO UPDATE SET
                  o = excluded.o, h = excluded.h, l = excluded.l, c = excluded.c, v = excluded.v;
                """;
            var pS = cmd.Parameters.Add("$s", SqliteType.Text);
            var pTf = cmd.Parameters.Add("$tf", SqliteType.Text);
            var pTs = cmd.Parameters.Add("$ts", SqliteType.Integer);
            var pO = cmd.Parameters.Add("$o", SqliteType.Real);
            var pH = cmd.Parameters.Add("$h", SqliteType.Real);
            var pL = cmd.Parameters.Add("$l", SqliteType.Real);
            var pC = cmd.Parameters.Add("$c", SqliteType.Real);
            var pV = cmd.Parameters.Add("$v", SqliteType.Real);

            pS.Value = symbol;
            pTf.Value = tf.ToString();
            for (int i = 0; i < count; i++)
            {
                var bar = candles[offset + i];
                pTs.Value = bar.Timestamp.ToUnixTimeMilliseconds();
                pO.Value = bar.Open;
                pH.Value = bar.High;
                pL.Value = bar.Low;
                pC.Value = bar.Close;
                pV.Value = bar.Volume;
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }
    }

    public IReadOnlyList<Candle> ReadCandles(string symbol, Timeframe tf, int? limit = null)
    {
        using var cmd = Conn.CreateCommand();
        cmd.CommandText = limit is null
            ? """
              SELECT ts_ms, o, h, l, c, v FROM Candles
              WHERE symbol = $s AND tf = $tf
              ORDER BY ts_ms ASC;
              """
            : """
              SELECT ts_ms, o, h, l, c, v FROM Candles
              WHERE symbol = $s AND tf = $tf
              ORDER BY ts_ms ASC
              LIMIT $lim;
              """;
        cmd.Parameters.AddWithValue("$s", symbol);
        cmd.Parameters.AddWithValue("$tf", tf.ToString());
        if (limit is not null)
            cmd.Parameters.AddWithValue("$lim", limit.Value);

        var list = new List<Candle>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            long ts = r.GetInt64(0);
            list.Add(new Candle(
                DateTimeOffset.FromUnixTimeMilliseconds(ts),
                r.GetDouble(1), r.GetDouble(2), r.GetDouble(3), r.GetDouble(4), r.GetDouble(5)));
        }
        return list;
    }

    public void WriteTicks(string symbol, IReadOnlyList<(DateTimeOffset Ts, double Bid, double Ask, double Vol)> ticks)
    {
        if (ticks.Count == 0) return;
        for (int offset = 0; offset < ticks.Count; offset += BatchRows)
        {
            int count = Math.Min(BatchRows, ticks.Count - offset);
            using var tx = Conn.BeginTransaction();
            using var cmd = Conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO Ticks(symbol, ts_ms, bid, ask, vol)
                VALUES ($s, $ts, $b, $a, $v);
                """;
            var pS = cmd.Parameters.Add("$s", SqliteType.Text);
            var pTs = cmd.Parameters.Add("$ts", SqliteType.Integer);
            var pB = cmd.Parameters.Add("$b", SqliteType.Real);
            var pA = cmd.Parameters.Add("$a", SqliteType.Real);
            var pV = cmd.Parameters.Add("$v", SqliteType.Real);
            pS.Value = symbol;
            for (int i = 0; i < count; i++)
            {
                var t = ticks[offset + i];
                pTs.Value = t.Ts.ToUnixTimeMilliseconds();
                pB.Value = t.Bid;
                pA.Value = t.Ask;
                pV.Value = t.Vol;
                cmd.ExecuteNonQuery();
            }
            tx.Commit();
        }
    }

    public int PurgeOldTicks(DateTimeOffset? nowUtc = null)
    {
        var cutoff = (nowUtc ?? DateTimeOffset.UtcNow).AddDays(-TickRetentionDays).ToUnixTimeMilliseconds();
        using var cmd = Conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Ticks WHERE ts_ms < $c;";
        cmd.Parameters.AddWithValue("$c", cutoff);
        return cmd.ExecuteNonQuery();
    }

    public long CountCandles(string symbol, Timeframe tf)
    {
        using var cmd = Conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Candles WHERE symbol = $s AND tf = $tf;";
        cmd.Parameters.AddWithValue("$s", symbol);
        cmd.Parameters.AddWithValue("$tf", tf.ToString());
        return (long)(cmd.ExecuteScalar() ?? 0L);
    }

    public long CountTicks(string symbol)
    {
        using var cmd = Conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM Ticks WHERE symbol = $s;";
        cmd.Parameters.AddWithValue("$s", symbol);
        return (long)(cmd.ExecuteScalar() ?? 0L);
    }

    public void SaveLayout(string name, string json)
    {
        using var cmd = Conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Layouts(name, json) VALUES ($n, $j)
            ON CONFLICT(name) DO UPDATE SET json = excluded.json;
            """;
        cmd.Parameters.AddWithValue("$n", name);
        cmd.Parameters.AddWithValue("$j", json);
        cmd.ExecuteNonQuery();
    }

    public string? LoadLayout(string name)
    {
        using var cmd = Conn.CreateCommand();
        cmd.CommandText = "SELECT json FROM Layouts WHERE name = $n;";
        cmd.Parameters.AddWithValue("$n", name);
        return cmd.ExecuteScalar() as string;
    }

    public void Dispose()
    {
        _conn?.Dispose();
        _conn = null;
    }
}
