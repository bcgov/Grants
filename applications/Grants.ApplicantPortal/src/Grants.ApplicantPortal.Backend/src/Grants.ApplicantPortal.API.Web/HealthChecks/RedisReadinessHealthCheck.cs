using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Grants.ApplicantPortal.API.Web.HealthChecks;

public class RedisReadinessHealthCheck(
    ILogger<RedisReadinessHealthCheck> logger,
    IConfiguration configuration,
    IDistributedCache distributedCache,
    IServiceProvider serviceProvider) : IHealthCheck
{
    private const string ProbeKey = "health:readiness:redis";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            return HealthCheckResult.Healthy("Redis not configured; running in in-memory mode");
        }

        // Hard cap each probe at 5 s. Without this, a hung Redis connection (e.g. during a
        // Sentinel failover where the client is still waiting to re-target the new master)
        // blocks a thread-pool thread indefinitely. OpenShift fires the readiness probe every
        // few seconds, so multiple hung tasks pile up, starve the thread pool, and eventually
        // prevent even the liveness probe from responding — causing the pod to restart in a loop.
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5));
        var probeToken = cts.Token;

        try
        {
            var multiplexer = serviceProvider.GetService<IConnectionMultiplexer>();
            if (multiplexer is null)
            {
                return HealthCheckResult.Degraded("Redis multiplexer is not registered");
            }

            // Always attempt a real round-trip — IsConnected can return true even when the
            // operation backlog is saturated after an overnight Redis pod recycle.
            var marker = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            await distributedCache.SetStringAsync(
                ProbeKey,
                marker,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
                },
                probeToken);

            var value = await distributedCache.GetStringAsync(ProbeKey, probeToken);
            if (value != marker)
            {
                return HealthCheckResult.Degraded("Redis cache probe round-trip returned unexpected value");
            }

            return HealthCheckResult.Healthy("Redis cache round-trip verified");
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Our 5 s internal timeout fired — Redis is hung, not the app shutting down.
            // Failing fast here is what prevents thread-pool starvation: without this the
            // blocked SetStringAsync/GetStringAsync tasks pile up across probe ticks and
            // eventually starve even the liveness endpoint, causing a pod restart loop.
            logger.LogWarning(ex, "Redis readiness check timed out after 5 s (connection hung — Sentinel failover or pod recycle in progress)");
            return HealthCheckResult.Degraded("Redis readiness check timed out");
        }
        catch (RedisConnectionException ex)
        {
            // Transient connectivity loss during a Sentinel failover or Redis pod recycle.
            // Log at Warning (not Error) — consistent with RedisDistributedLock behaviour —
            // and return Degraded so the readiness probe marks the pod temporarily unready
            // rather than triggering an alert storm.
            logger.LogWarning(ex, "Redis connection lost during readiness check (transient — Sentinel failover or pod recycle)");
            return HealthCheckResult.Degraded("Redis temporarily unavailable (connection lost)", ex);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("READONLY", StringComparison.OrdinalIgnoreCase))
        {
            // After a Sentinel failover the old master becomes a replica and rejects writes.
            // The multiplexer's ExponentialRetry reconnect policy (wired in RedisConnectionOptionsFactory)
            // will re-target the new master shortly; treat this as transient degradation.
            logger.LogWarning(ex, "Redis write rejected — node is READONLY (Sentinel failover in progress)");
            return HealthCheckResult.Degraded("Redis READONLY — Sentinel failover in progress", ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Redis readiness check failed");
            return HealthCheckResult.Unhealthy("Redis readiness check failed", ex);
        }
    }
}
