using Grants.ApplicantPortal.API.Infrastructure.Messaging.Configuration;
using Grants.ApplicantPortal.API.UseCases;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using StackExchange.Redis;

namespace Grants.ApplicantPortal.API.Web.Configurations;

public static class CacheConfigs
{
  public static IServiceCollection AddCacheConfigs(this IServiceCollection services, WebApplicationBuilder builder, Microsoft.Extensions.Logging.ILogger logger)
  {
    var cacheConfig = builder.Configuration.GetSection("ProfileCache").Get<ProfileCacheOptions>()
                      ?? new ProfileCacheOptions();

    var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
    logger.LogInformation("Redis connection string configured: {HasRedis}", !string.IsNullOrEmpty(redisConnectionString));

    if (!string.IsNullOrEmpty(redisConnectionString))
    {
      // Register the multiplexer as a singleton if messaging services haven't done so yet.
      // Having one shared instance means IDistributedCache, IConnectionMultiplexer (distributed
      // locking, health check), and ReconfigureAsync recovery calls all share the same
      // Sentinel-managed connection.
      if (!services.Any(x => x.ServiceType == typeof(IConnectionMultiplexer)))
      {
        var redisOptions = RedisConnectionOptionsFactory.Create(redisConnectionString, logger, "distributed cache");
        var mux = ConnectionMultiplexer.Connect(redisOptions);
        RedisConnectionOptionsFactory.Subscribe(mux, logger);
        services.AddSingleton<IConnectionMultiplexer>(mux);
      }

      // Wire the cache to the shared singleton rather than creating its own internal multiplexer.
      // Post-configure runs after the container is built, so IConnectionMultiplexer resolves
      // correctly regardless of registration order.
      services.AddStackExchangeRedisCache(options => options.InstanceName = "ApplicantPortal");
      services.AddOptions<RedisCacheOptions>()
          .Configure<IConnectionMultiplexer>((options, mux) =>
              options.ConnectionMultiplexerFactory = () => Task.FromResult(mux));

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
