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

        try
        {
            var multiplexer = serviceProvider.GetService<IConnectionMultiplexer>();
            if (multiplexer is not null && !multiplexer.IsConnected)
            {
                return HealthCheckResult.Degraded("Redis multiplexer is disconnected");
            }

            var marker = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            await distributedCache.SetStringAsync(
                ProbeKey,
                marker,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30)
                },
                cancellationToken);

            var value = await distributedCache.GetStringAsync(ProbeKey, cancellationToken);
            if (value != marker)
            {
                return HealthCheckResult.Degraded("Redis cache probe round-trip returned unexpected value");
            }

            return HealthCheckResult.Healthy("Redis cache round-trip verified");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Redis readiness check failed");
            return HealthCheckResult.Unhealthy("Redis readiness check failed", ex);
        }
    }
}
