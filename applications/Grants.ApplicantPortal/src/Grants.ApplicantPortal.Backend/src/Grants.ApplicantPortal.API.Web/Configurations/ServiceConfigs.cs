using Grants.ApplicantPortal.API.Infrastructure;
using Grants.ApplicantPortal.API.Core.Features;
using Grants.ApplicantPortal.API.Plugins;
using Grants.ApplicantPortal.API.UseCases;
using Grants.ApplicantPortal.API.Web.HealthChecks;
using Grants.ApplicantPortal.API.Web.Profiles;

namespace Grants.ApplicantPortal.API.Web.Configurations;

public static class ServiceConfigs
{
  public static IServiceCollection AddServiceConfigs(this IServiceCollection services, Microsoft.Extensions.Logging.ILogger logger, WebApplicationBuilder builder)
  {
    services.AddInfrastructureServices(builder.Configuration, logger)
            .AddUseCaseServices(logger)            
            .AddFeatureServices(logger)
            .AddPluginServices(logger)
            .AddMediatrConfigs()
            .AddKeycloakAuthentication(builder.Configuration, logger)
            .AddAuthorizationPolicies(logger)
            .AddCorsConfigs(builder, logger)
            .AddCacheConfigs(builder, logger);

    services.AddScoped<IProfileService, ProfileService>();

    services.AddHealthChecks()
            .AddCheck<DatabaseReadinessHealthCheck>("database-readiness", tags: ["ready"], timeout: TimeSpan.FromSeconds(10))
            .AddCheck<RedisReadinessHealthCheck>("redis-readiness", tags: ["ready"], timeout: TimeSpan.FromSeconds(10));

    logger.LogInformation("{Project} services registered", "Mediatr, Authentication, Authorization, CORS and HybridCache");

    return services;
  }
}
