namespace MyChart.Infrastructure.Providers.MetaTrader;

/// <summary>
/// Bidirectional in-memory transport for tests (fake pipe server).
/// Pair two instances with CreatePair.
/// </summary>
public sealed class InMemoryMtTransport : IMtTransport
{
    private InMemoryMtTransport? _peer;
    private int _connected;

    public bool IsConnected => Volatile.Read(ref _connected) == 1;

    public event Action<string>? LineReceived;

    public static (InMemoryMtTransport app, InMemoryMtTransport ea) CreatePair()
    {
        var a = new InMemoryMtTransport();
        var b = new InMemoryMtTransport();
        a._peer = b;
        b._peer = a;
        return (a, b);
    }

    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        Interlocked.Exchange(ref _connected, 1);
        return Task.CompletedTask;
    }

    public Task DisconnectAsync()
    {
        Interlocked.Exchange(ref _connected, 0);
        return Task.CompletedTask;
    }

    public Task SendLineAsync(string line, CancellationToken cancellationToken)
    {
        if (!IsConnected)
            throw new InvalidOperationException("Transport is not connected.");
        _peer?.LineReceived?.Invoke(line);
        return Task.CompletedTask;
    }

    /// <summary>Simulate the peer sending a line to this side (test helper).</summary>
    public void InjectLine(string line) => LineReceived?.Invoke(line);

    public ValueTask DisposeAsync() => new(DisconnectAsync());
}
