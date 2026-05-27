using StackExchange.Redis;

namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.Configuration;

public static class RedisConnectionOptionsFactory
{
    public static ConfigurationOptions Create(string connectionString, ILogger logger, string purpose)
    {
        var options = ConfigurationOptions.Parse(connectionString);

        options.AbortOnConnectFail = false;
        options.ConnectRetry = Math.Max(options.ConnectRetry, 5);
        options.ConnectTimeout = Math.Max(options.ConnectTimeout, 10000);
        options.SyncTimeout = Math.Max(options.SyncTimeout, 5000);
        // Cap at 10 s so retries don't stall for minutes after a Sentinel failover.
        options.ReconnectRetryPolicy = new ExponentialRetry(1000, 10000);
        // Probe idle connections every 30 s so the multiplexer detects dead sockets
        // (e.g. after a Sentinel failover or overnight pod recycle) before the next operation.
        options.KeepAlive = 30;
        // Re-check Sentinel master address every 15 s (default 60 s) so a newly elected
        // master is discovered 4x faster without waiting for the full default interval.
        options.ConfigCheckSeconds = 15;

        logger.LogInformation(
            "Redis options prepared for {Purpose}. Sentinel: {IsSentinel}, ServiceName: {ServiceName}, Endpoints: {Endpoints}",
            purpose,
            !string.IsNullOrWhiteSpace(options.ServiceName),
            string.IsNullOrWhiteSpace(options.ServiceName) ? "(none)" : options.ServiceName,
            string.Join(", ", options.EndPoints.Select(ep => ep.ToString())));

        return options;
    }

    /// <summary>
    /// Wires connection-lifecycle events on a freshly created multiplexer.
    /// Call once after <see cref="ConnectionMultiplexer.Connect"/> before registering in DI.
    /// </summary>
    public static void Subscribe(IConnectionMultiplexer multiplexer, ILogger logger)
    {
        multiplexer.ConnectionFailed += (_, e) =>
            // Log only — do NOT call ReconfigureAsync here.
            // During a Sentinel failover this event fires 6 times in rapid succession
            // (Interactive + Subscription × 3 nodes). Calling ReconfigureAsync 6 times
            // concurrently races against StackExchange.Redis's own internal +switch-master
            // pub/sub handling and leaves the master endpoint inconsistent for minutes.
            // The library manages Sentinel re-discovery automatically via +switch-master
            // and the ConfigCheckSeconds = 15 periodic re-check.
            logger.LogWarning(
                "Redis connection failed: {EndPoint} ({FailureType}, {ConnectionType})",
                e.EndPoint, e.FailureType, e.ConnectionType);

        multiplexer.ConnectionRestored += (_, e) =>
            logger.LogInformation(
                "Redis connection restored: {EndPoint} ({ConnectionType})",
                e.EndPoint, e.ConnectionType);

        multiplexer.ErrorMessage += (_, e) =>
            logger.LogWarning("Redis server error from {EndPoint}: {Message}", e.EndPoint, e.Message);
    }
}
