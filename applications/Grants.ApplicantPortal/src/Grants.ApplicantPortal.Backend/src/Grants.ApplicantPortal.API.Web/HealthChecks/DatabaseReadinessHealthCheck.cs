using Grants.ApplicantPortal.API.Infrastructure.Data;
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

            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy("Database is not reachable");
            }

            return HealthCheckResult.Healthy("Database connectivity verified");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database readiness check failed");
            return HealthCheckResult.Unhealthy("Database readiness check failed", ex);
        }
    }
}
