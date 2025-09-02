using Grants.ApplicantPortal.API.Core.Email;
using Grants.ApplicantPortal.API.Infrastructure;
using Grants.ApplicantPortal.API.Infrastructure.Email;
using Grants.ApplicantPortal.API.Core.Features;
using Grants.ApplicantPortal.API.Plugins;

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
            .AddFeatureServices(logger)
            .AddPluginServices(logger)
            .AddMediatrConfigs()
            .AddKeycloakAuthentication(builder.Configuration, logger)
            .AddAuthorizationPolicies(logger)
            .AddCorsConfigs(builder, logger)
            .AddCacheConfigs(builder, logger);

    if (builder.Environment.IsDevelopment())
    {
      // Use a local test email server
      // See: https://ardalis.com/configuring-a-local-test-email-server/
      services.AddScoped<IEmailSender, MimeKitEmailSender>();

      // Otherwise use this:
      //builder.Services.AddScoped<IEmailSender, FakeEmailSender>();

    }
    else
    {
      services.AddScoped<IEmailSender, MimeKitEmailSender>();
    }

    logger.LogInformation("{Project} services registered", "Mediatr, Email Sender, Authentication, Authorization, CORS and HybridCache");

    return services;
  }
}
