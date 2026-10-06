namespace MyChart.Infrastructure.Providers.MetaTrader;

/// <summary>
/// Abstraction over named pipe / TCP so tests can inject a fake transport.
/// Lines are complete messages without trailing newline.
/// </summary>
public interface IMtTransport : IAsyncDisposable
{
    bool IsConnected { get; }
    Task ConnectAsync(CancellationToken cancellationToken);
    Task DisconnectAsync();
    Task SendLineAsync(string line, CancellationToken cancellationToken);

    /// <summary>Raised when a full line is received from the peer.</summary>
    event Action<string>? LineReceived;
}
