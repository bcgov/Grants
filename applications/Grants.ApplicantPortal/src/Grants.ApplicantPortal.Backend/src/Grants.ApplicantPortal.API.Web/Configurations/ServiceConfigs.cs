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
    // Add temporary diagnostic logging
    logger.LogInformation("=== ENVIRONMENT DIAGNOSTICS ===");
    logger.LogInformation("Environment Name: {EnvironmentName}", builder.Environment.EnvironmentName);
    logger.LogInformation("Is Development: {IsDevelopment}", builder.Environment.IsDevelopment());
    logger.LogInformation("ASPNETCORE_ENVIRONMENT: {AspNetCoreEnvironment}", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
    
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
    logger.LogInformation("Redis Connection String: {RedisConnectionString}", string.IsNullOrEmpty(redisConnectionString) ? "NOT SET" : redisConnectionString);
    logger.LogInformation("=== END DIAGNOSTICS ===");

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
            .AddCheck<DatabaseReadinessHealthCheck>("database-readiness", tags: ["ready"])
            .AddCheck<RedisReadinessHealthCheck>("redis-readiness", tags: ["ready"]);

    logger.LogInformation("{Project} services registered", "Mediatr, Authentication, Authorization, CORS and HybridCache");

    return services;
  }
}
