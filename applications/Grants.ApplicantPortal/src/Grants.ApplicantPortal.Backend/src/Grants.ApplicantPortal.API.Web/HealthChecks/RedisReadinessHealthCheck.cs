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
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            return HealthCheckResult.Healthy("Redis not configured; running in in-memory mode");
        }

        var multiplexer = serviceProvider.GetService<IConnectionMultiplexer>();
        if (multiplexer is null)
        {
            return HealthCheckResult.Unhealthy("Redis multiplexer is not registered");
        }

        // Per-invocation key prevents concurrent probes from overwriting each other's
        // marker and producing a false negative on the round-trip value check.
        var probeKey = $"health:readiness:redis:{Guid.NewGuid():N}";

        // Hard cap each probe at 5 s so a hung Redis connection (e.g. during a Sentinel failover)
        // doesn't block a thread-pool thread for the full framework timeout (10 s).
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5));
        var probeToken = cts.Token;

        try
        {
            // Always attempt a real round-trip — IsConnected can return true even when the
            // operation backlog is saturated after an overnight Redis pod recycle.
            var marker = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            await distributedCache.SetStringAsync(
                probeKey,
                marker,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
                },
                probeToken);

            var value = await distributedCache.GetStringAsync(probeKey, probeToken);
            if (value != marker)
            {
                return HealthCheckResult.Unhealthy("Redis cache probe round-trip returned unexpected value");
            }

            return HealthCheckResult.Healthy("Redis cache round-trip verified");
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Our 5 s internal timeout fired — Redis is hung, not the app shutting down.
            // Unhealthy (not Degraded) so the readiness probe returns 503 and the pod is
            // temporarily removed from the load balancer, preventing hung requests from
            // piling up on callers. The liveness probe does not check Redis so the pod
            // is not restarted.
            logger.LogWarning(ex, "Redis readiness check timed out after 5 s (connection hung — Sentinel failover or pod recycle in progress)");
            return HealthCheckResult.Unhealthy("Redis readiness check timed out");
        }
        catch (RedisConnectionException ex)
        {
            logger.LogWarning(ex, "Redis connection lost during readiness check (transient — Sentinel failover or pod recycle)");
            // Kick Sentinel re-discovery so the multiplexer re-targets the new master rather
            // than waiting for the next ConfigCheckSeconds tick (15 s) or ExponentialRetry interval.
            // ReconfigureAsync lives on the concrete class, not the interface.
            if (multiplexer is ConnectionMultiplexer cm)
            {
                try
                {
                    var reconfig = await cm.ReconfigureAsync("readiness-check-connection-loss");
                    logger.LogInformation("Redis ReconfigureAsync returned {Result} (true = master endpoint updated)", reconfig);
                }
                catch (Exception reconfigEx)
                {
                    logger.LogWarning(reconfigEx, "Redis ReconfigureAsync faulted (Sentinel may also be unavailable)");
                }
            }
            return HealthCheckResult.Unhealthy("Redis temporarily unavailable (connection lost)", ex);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("READONLY", StringComparison.OrdinalIgnoreCase))
        {
            // After a Sentinel failover the old master becomes a replica and rejects writes.
            // Unhealthy removes the pod from rotation until the multiplexer re-targets
            // the new master via ExponentialRetry / ConfigCheckSeconds.
            logger.LogWarning(ex, "Redis write rejected — node is READONLY (Sentinel failover in progress)");
            return HealthCheckResult.Unhealthy("Redis READONLY — Sentinel failover in progress", ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Redis readiness check failed");
            return HealthCheckResult.Unhealthy("Redis readiness check failed", ex);
        }
    }
}
