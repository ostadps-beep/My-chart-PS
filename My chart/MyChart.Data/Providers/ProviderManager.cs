using MyChart.Core.Contracts.Data;

namespace MyChart.Data.Providers;

/// <summary>
/// T5.02 ProviderManager — register by name, switch, health, reconnect backoff.
/// Heartbeat every 5 s; 15 s without answer = Disconnected.
/// Backoff: 1,2,4,8,16,30 then 30 forever. Failover after 3 failed reconnects.
/// </summary>
public sealed class ProviderManager : IDisposable
{
    public static readonly int[] BackoffSeconds = { 1, 2, 4, 8, 16, 30 };

    private readonly Dictionary<string, IDataProvider> _providers = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();
    private string? _activeName;
    private string? _secondaryName;
    private int _failedReconnects;
    private int _backoffIndex;
    private DateTimeOffset _lastHeartbeatUtc = DateTimeOffset.MinValue;
    private ProviderConnectionState _state = ProviderConnectionState.Disconnected;
    private bool _disposed;

    /// <summary>Injectable clock for tests (default UtcNow).</summary>
    public Func<DateTimeOffset> UtcNow { get; set; } = () => DateTimeOffset.UtcNow;

    public event Action<string?, string?>? ProviderChanged;
    public event Action<ProviderConnectionState>? StateChanged;

    public ProviderConnectionState State
    {
        get { lock (_gate) return _state; }
        private set
        {
            if (_state == value) return;
            _state = value;
            StateChanged?.Invoke(value);
        }
    }

    public IDataProvider? Active
    {
        get
        {
            lock (_gate)
                return _activeName is not null && _providers.TryGetValue(_activeName, out var p) ? p : null;
        }
    }

    public string? ActiveName
    {
        get { lock (_gate) return _activeName; }
    }

    public int FailedReconnects
    {
        get { lock (_gate) return _failedReconnects; }
    }

    public void Register(IDataProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        lock (_gate)
            _providers[provider.Name] = provider;
    }

    public void SetSecondary(string? name)
    {
        lock (_gate) _secondaryName = name;
    }

    public async Task SwitchAsync(string name, CancellationToken ct = default)
    {
        IDataProvider next;
        IDataProvider? old;
        string? oldName;
        lock (_gate)
        {
            if (!_providers.TryGetValue(name, out next!))
                throw new KeyNotFoundException($"Provider '{name}' is not registered.");
            oldName = _activeName;
            old = oldName is not null && _providers.TryGetValue(oldName, out var o) ? o : null;
            _activeName = name;
            _failedReconnects = 0;
            _backoffIndex = 0;
            State = ProviderConnectionState.Connecting;
        }

        if (old is not null && !ReferenceEquals(old, next))
            await old.DisconnectAsync().ConfigureAwait(false);

        await next.ConnectAsync(ct).ConfigureAwait(false);
        lock (_gate)
        {
            _lastHeartbeatUtc = UtcNow();
            State = ProviderConnectionState.Connected;
        }
        ProviderChanged?.Invoke(oldName, name);
    }

    /// <summary>Record a successful heartbeat/pong from the active provider.</summary>
    public void NoteHeartbeat()
    {
        lock (_gate)
        {
            _lastHeartbeatUtc = UtcNow();
            if (State is ProviderConnectionState.Reconnecting or ProviderConnectionState.Connecting)
                State = ProviderConnectionState.Connected;
            _failedReconnects = 0;
            _backoffIndex = 0;
        }
    }

    /// <summary>
    /// Health check: if last heartbeat older than 15 s, mark Disconnected and schedule reconnect logic.
    /// Returns the next backoff delay in seconds, or 0 if still healthy.
    /// </summary>
    public int CheckHealth()
    {
        lock (_gate)
        {
            if (_activeName is null) return 0;
            var age = UtcNow() - _lastHeartbeatUtc;
            if (age.TotalSeconds < 15)
                return 0;

            State = ProviderConnectionState.Disconnected;
            return NextBackoffSecondsLocked();
        }
    }

    /// <summary>Attempt reconnect of active provider. On failure increments backoff.</summary>
    public async Task<bool> TryReconnectAsync(CancellationToken ct = default)
    {
        IDataProvider? provider;
        lock (_gate)
        {
            if (_activeName is null || !_providers.TryGetValue(_activeName, out provider))
                return false;
            State = ProviderConnectionState.Reconnecting;
        }

        try
        {
            await provider!.ConnectAsync(ct).ConfigureAwait(false);
            lock (_gate)
            {
                _lastHeartbeatUtc = UtcNow();
                _failedReconnects = 0;
                _backoffIndex = 0;
                State = ProviderConnectionState.Connected;
            }
            return true;
        }
        catch
        {
            lock (_gate)
            {
                _failedReconnects++;
                State = ProviderConnectionState.Disconnected;
                if (_failedReconnects >= 3 && _secondaryName is not null
                    && _providers.ContainsKey(_secondaryName)
                    && !string.Equals(_secondaryName, _activeName, StringComparison.OrdinalIgnoreCase))
                {
                    // Failover: caller should SwitchAsync(secondary)
                }
            }
            return false;
        }
    }

    public bool ShouldFailover()
    {
        lock (_gate)
            return _failedReconnects >= 3
                   && _secondaryName is not null
                   && !string.Equals(_secondaryName, _activeName, StringComparison.OrdinalIgnoreCase);
    }

    public string? SecondaryName
    {
        get { lock (_gate) return _secondaryName; }
    }

    public int NextBackoffSeconds()
    {
        lock (_gate) return NextBackoffSecondsLocked();
    }

    private int NextBackoffSecondsLocked()
    {
        int idx = Math.Min(_backoffIndex, BackoffSeconds.Length - 1);
        int delay = BackoffSeconds[idx];
        if (_backoffIndex < BackoffSeconds.Length - 1)
            _backoffIndex++;
        return delay;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var p in _providers.Values)
        {
            try { p.DisconnectAsync().GetAwaiter().GetResult(); } catch { /* ignore */ }
        }
    }
}
