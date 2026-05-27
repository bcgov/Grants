using System.Net;
using StackExchange.Redis;
using StackExchange.Redis.Maintenance;
using StackExchange.Redis.Profiling;

namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.Configuration;

/// <summary>
/// Wraps a <see cref="ConnectionMultiplexer"/> and atomically recreates it after
/// <see cref="FailureThreshold"/> consecutive readiness-probe failures.
///
/// Root cause this solves: after a Sentinel failover the old master becomes a replica but
/// StackExchange.Redis keeps a live TCP socket to it. Because the socket is alive, SE.Redis
/// never triggers a reconnect, and <c>ConfigCheckSeconds</c> / <c>ReconfigureAsync</c> only
/// update the metadata — the write-connection is NOT switched to the new master. This leads to
/// indefinite READONLY errors with no self-recovery. Recreating the multiplexer from scratch
/// forces a fresh Sentinel <c>get-master-addr-by-name</c> query and connects to the correct master.
/// </summary>
public sealed class ResettableConnectionMultiplexer : IConnectionMultiplexer
{
    // volatile ensures reads always observe the latest reference without caching.
    // Writes use Interlocked.Exchange which provides its own memory barrier.
#pragma warning disable CS0420 // volatile field passed by-ref to Interlocked — safe, Interlocked provides the barrier
    private volatile ConnectionMultiplexer _inner;
#pragma warning restore CS0420
    private readonly ConfigurationOptions _options;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _recreateLock = new(1, 1);
    private int _consecutiveFailures;

    public const int FailureThreshold = 3;

    public ResettableConnectionMultiplexer(ConnectionMultiplexer inner, ConfigurationOptions options, ILogger logger)
    {
        _inner = inner;
        _options = options;
        _logger = logger;
        ForwardEventsFrom(inner);
    }

    // ── Failure tracking ─────────────────────────────────────────────────────

    public void RecordSuccess() => Interlocked.Exchange(ref _consecutiveFailures, 0);

    public void RecordFailure()
    {
        var count = Interlocked.Increment(ref _consecutiveFailures);
        _logger.LogDebug(
            "ResettableConnectionMultiplexer: probe failure {Count}/{Threshold}",
            count, FailureThreshold);
        if (count >= FailureThreshold)
            _ = RecreateAsync();
    }

    /// <summary>
    /// Delegates to the inner multiplexer's <c>ReconfigureAsync</c> (Sentinel re-discovery).
    /// Not part of <see cref="IConnectionMultiplexer"/> — call via this concrete type.
    /// </summary>
    public Task<bool> ReconfigureAsync(string reason) => _inner.ReconfigureAsync(reason);

    public async Task RecreateAsync()
    {
        if (!await _recreateLock.WaitAsync(0))
        {
            _logger.LogDebug("ResettableConnectionMultiplexer: recreation already in progress — skipping");
            return;
        }

        try
        {
            _logger.LogWarning(
                "ResettableConnectionMultiplexer: recreating inner multiplexer after {Failures} consecutive probe failures",
                _consecutiveFailures);

            ConnectionMultiplexer newMux;
            try
            {
                newMux = await ConnectionMultiplexer.ConnectAsync(_options);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "ResettableConnectionMultiplexer: failed to connect replacement multiplexer — retaining existing connection");
                return;
            }

            ForwardEventsFrom(newMux);
#pragma warning disable CS0420
            var old = Interlocked.Exchange(ref _inner, newMux);
#pragma warning restore CS0420
            Interlocked.Exchange(ref _consecutiveFailures, 0);

            _logger.LogInformation(
                "ResettableConnectionMultiplexer: inner multiplexer recreated — fresh Sentinel discovery complete");

            // Brief pause to allow in-flight operations to drain before disposing the old socket
            await Task.Delay(500);
            try
            {
                await old.CloseAsync(allowCommandsToComplete: false);
                await old.DisposeAsync();
            }
            catch (Exception ex)
            {
                // Best-effort: disposal errors on the replaced multiplexer must not fail the recreation.
                _logger.LogWarning(ex, "ResettableConnectionMultiplexer: error disposing replaced inner multiplexer (non-fatal)");
            }
        }
        finally
        {
            _recreateLock.Release();
        }
    }

    private void ForwardEventsFrom(ConnectionMultiplexer mux)
    {
        mux.ConnectionFailed += (_, e) => ConnectionFailed?.Invoke(this, e);
        mux.ConnectionRestored += (_, e) => ConnectionRestored?.Invoke(this, e);
        mux.ErrorMessage += (_, e) => ErrorMessage?.Invoke(this, e);
        mux.InternalError += (_, e) => InternalError?.Invoke(this, e);
        mux.ConfigurationChanged += (_, e) => ConfigurationChanged?.Invoke(this, e);
        mux.ConfigurationChangedBroadcast += (_, e) => ConfigurationChangedBroadcast?.Invoke(this, e);
        mux.HashSlotMoved += (_, e) => HashSlotMoved?.Invoke(this, e);
        // Named handler: the event and type share the name 'ServerMaintenanceEvent'; a dedicated
        // raise method lets SonarQube S3264 track the invocation correctly.
        mux.ServerMaintenanceEvent += OnServerMaintenanceEvent;
    }

    // Dedicated raise method needed because the SE.Redis type 'ServerMaintenanceEvent' and
    // the interface event share the same identifier — Sonar cannot track the invocation through
    // a lambda that uses this.ServerMaintenanceEvent?.Invoke().
#pragma warning disable S1172 // sender is mandated by the EventHandler<T> delegate signature
    private void OnServerMaintenanceEvent(object? _, StackExchange.Redis.Maintenance.ServerMaintenanceEvent e)
        => ServerMaintenanceEvent?.Invoke(this, e);
#pragma warning restore S1172

    // ── IConnectionMultiplexer events ─────────────────────────────────────────

    public event EventHandler<RedisErrorEventArgs>? ErrorMessage;
    public event EventHandler<ConnectionFailedEventArgs>? ConnectionFailed;
    public event EventHandler<InternalErrorEventArgs>? InternalError;
    public event EventHandler<ConnectionFailedEventArgs>? ConnectionRestored;
    public event EventHandler<EndPointEventArgs>? ConfigurationChanged;
    public event EventHandler<EndPointEventArgs>? ConfigurationChangedBroadcast;
    public event EventHandler<HashSlotMovedEventArgs>? HashSlotMoved;
    public event EventHandler<StackExchange.Redis.Maintenance.ServerMaintenanceEvent>? ServerMaintenanceEvent;

    // ── IConnectionMultiplexer properties ────────────────────────────────────

    public string ClientName => _inner.ClientName;
    public string Configuration => _inner.Configuration;
    public int TimeoutMilliseconds => _inner.TimeoutMilliseconds;
    public long OperationCount => _inner.OperationCount;
    public bool IsConnected => _inner.IsConnected;
    public bool IsConnecting => _inner.IsConnecting;
#pragma warning disable CS0618 // Both properties are obsolete on the concrete class but still required by IConnectionMultiplexer; will be removed in SE.Redis 3.0
    public bool PreserveAsyncOrder { get => _inner.PreserveAsyncOrder; set => _inner.PreserveAsyncOrder = value; }
    public bool IncludeDetailInExceptions { get => _inner.IncludeDetailInExceptions; set => _inner.IncludeDetailInExceptions = value; }
#pragma warning restore CS0618
    public int StormLogThreshold { get => _inner.StormLogThreshold; set => _inner.StormLogThreshold = value; }

    // ── IConnectionMultiplexer methods ────────────────────────────────────────

    public void Close(bool allowCommandsToComplete = true) => _inner.Close(allowCommandsToComplete);
    public Task CloseAsync(bool allowCommandsToComplete = true) => _inner.CloseAsync(allowCommandsToComplete);
    public bool Configure(TextWriter? log = null) => _inner.Configure(log);
    public Task<bool> ConfigureAsync(TextWriter? log = null) => _inner.ConfigureAsync(log);
    public void Dispose() { _recreateLock.Dispose(); _inner.Dispose(); }
    public ValueTask DisposeAsync() => _inner.DisposeAsync();
    public ServerCounters GetCounters() => _inner.GetCounters();
    public IDatabase GetDatabase(int db = -1, object? asyncState = null) => _inner.GetDatabase(db, asyncState);
    public EndPoint[] GetEndPoints(bool configuredOnly = false) => _inner.GetEndPoints(configuredOnly);
    public IServer[] GetServers() => _inner.GetServers();
    public IServer GetServer(string host, int port, object? asyncState = null) => _inner.GetServer(host, port, asyncState);
    public IServer GetServer(string hostAndPort, object? asyncState = null) => _inner.GetServer(hostAndPort, asyncState);
    public IServer GetServer(IPAddress host, int port) => _inner.GetServer(host, port);
    public IServer GetServer(EndPoint endpoint, object? asyncState = null) => _inner.GetServer(endpoint, asyncState);
    public string GetStatus() => _inner.GetStatus();
    public void GetStatus(TextWriter log) => _inner.GetStatus(log);
    public string GetStormLog() => _inner.GetStormLog() ?? string.Empty;
    public ISubscriber GetSubscriber(object? asyncState = null) => _inner.GetSubscriber(asyncState);
    public int HashSlot(RedisKey key) => _inner.HashSlot(key);
    public int GetHashSlot(RedisKey key) => _inner.GetHashSlot(key);
    public void AddLibraryNameSuffix(string suffix) => _inner.AddLibraryNameSuffix(suffix);
    public void RegisterProfiler(Func<ProfilingSession?> profilingSessionProvider) => _inner.RegisterProfiler(profilingSessionProvider);
    public void ResetStormLog() => _inner.ResetStormLog();
    public long PublishReconfigure(CommandFlags flags = CommandFlags.None) => _inner.PublishReconfigure(flags);
    public Task<long> PublishReconfigureAsync(CommandFlags flags = CommandFlags.None) => _inner.PublishReconfigureAsync(flags);
    public void Wait(Task task) => _inner.Wait(task);
    public T Wait<T>(Task<T> task) => _inner.Wait(task);
    public void WaitAll(params Task[] tasks) => _inner.WaitAll(tasks);
    public void ExportConfiguration(Stream destination, ExportOptions options = ExportOptions.All) => _inner.ExportConfiguration(destination, options);
    public override string ToString() => _inner.ToString() ?? string.Empty;
}
