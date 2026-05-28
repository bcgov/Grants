using Grants.ApplicantPortal.API.Infrastructure.Messaging.Configuration;
using Grants.ApplicantPortal.API.UseCases;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Options;
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
        var resettableMux = new ResettableConnectionMultiplexer(mux, redisOptions, logger);
        // Subscribe to the wrapper — events are forwarded from inner so they survive recreation.
        RedisConnectionOptionsFactory.Subscribe(resettableMux, logger);
        services.AddSingleton<IConnectionMultiplexer>(resettableMux);
      }

      // Configure RedisCacheOptions manually (equivalent to AddStackExchangeRedisCache) so we can
      // register ResettableDistributedCache instead of the bare RedisCache singleton.
      // AddStackExchangeRedisCache = AddOptions() + Configure<RedisCacheOptions> + AddSingleton<IDistributedCache, RedisCache>
      services.AddOptions();
      services.Configure<RedisCacheOptions>(opts => opts.InstanceName = "ApplicantPortal");

      // Wire the cache to the shared multiplexer wrapper. Post-configure runs after the container
      // is built so IConnectionMultiplexer resolves correctly regardless of registration order.
      services.AddOptions<RedisCacheOptions>()
          .Configure<IConnectionMultiplexer>((options, mux) =>
              options.ConnectionMultiplexerFactory = () => Task.FromResult(mux));

      // ResettableDistributedCache wraps RedisCache and recovers from ObjectDisposedException that
      // occurs when the inner multiplexer is recreated (disposing the old one invalidates the
      // IDatabase that RedisCache cached internally). On disposal exception, a fresh RedisCache is
      // created via the factory, which re-fetches the database from the now-new multiplexer inner.
      services.AddSingleton<IDistributedCache>(sp =>
      {
        var cacheOpts = sp.GetRequiredService<IOptions<RedisCacheOptions>>();
        var cacheLogger = sp.GetRequiredService<ILogger<ResettableDistributedCache>>();
        return new ResettableDistributedCache(() => new RedisCache(cacheOpts), cacheLogger);
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
