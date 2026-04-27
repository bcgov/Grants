using Grants.ApplicantPortal.API.Infrastructure.Messaging.Configuration;
using Grants.ApplicantPortal.API.UseCases;

namespace Grants.ApplicantPortal.API.Web.Configurations;

public static class CacheConfigs
{
  public static IServiceCollection AddCacheConfigs(this IServiceCollection services, WebApplicationBuilder builder, Microsoft.Extensions.Logging.ILogger logger)
  {
    var cacheConfig = builder.Configuration.GetSection("ProfileCache").Get<ProfileCacheOptions>() 
                      ?? new ProfileCacheOptions();

    logger.LogInformation("Environment: {Environment}, IsDevelopment: {IsDevelopment}", 
        builder.Environment.EnvironmentName, builder.Environment.IsDevelopment());

    var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
    logger.LogInformation("Redis connection string configured: {HasRedis}", !string.IsNullOrEmpty(redisConnectionString));

    if (!string.IsNullOrEmpty(redisConnectionString))
    {
      var redisOptions = RedisConnectionOptionsFactory.Create(redisConnectionString, logger, "distributed cache");

      services.AddStackExchangeRedisCache(options =>
      {
        options.ConfigurationOptions = redisOptions;
        options.InstanceName = "ApplicantPortal";
      });

      logger.LogInformation("Redis distributed cache configured successfully");
    }
    else
    {
      services.AddDistributedMemoryCache();

      logger.LogInformation("Using DistributedMemoryCache (Redis not configured). Expiry: {CacheExpiry}min",
          cacheConfig.CacheExpiryMinutes);
    }

    logger.LogInformation("Distributed cache configuration completed");

    return services;
  }
}

