using Grants.ApplicantPortal.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Grants.ApplicantPortal.API.Web.HealthChecks;

public class DatabaseReadinessHealthCheck(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<DatabaseReadinessHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Open a fresh connection explicitly instead of relying on CanConnectAsync,
            // which may return a cached/pooled result and miss a stale pool after a DB recycle.
            // This ensures OpenShift's readiness probe detects dead connections and restarts the pod.
            var connection = dbContext.Database.GetDbConnection();
            await connection.OpenAsync(cancellationToken);
            await connection.CloseAsync();

            return HealthCheckResult.Healthy("Database connectivity verified");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database readiness check failed");
            return HealthCheckResult.Unhealthy("Database readiness check failed", ex);
        }
    }
}
