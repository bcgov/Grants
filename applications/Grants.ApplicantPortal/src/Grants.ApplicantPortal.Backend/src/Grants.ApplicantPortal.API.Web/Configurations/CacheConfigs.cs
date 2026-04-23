using Grants.ApplicantPortal.API.UseCases;
using StackExchange.Redis;

namespace Grants.ApplicantPortal.API.Web.Configurations;

public static class CacheConfigs
{
  public static IServiceCollection AddCacheConfigs(this IServiceCollection services, WebApplicationBuilder builder, Microsoft.Extensions.Logging.ILogger logger)
  {
    // Add distributed caching support with Redis as the backing store
    // Get cache configuration from appsettings
    var cacheConfig = builder.Configuration.GetSection("ProfileCache").Get<ProfileCacheOptions>() 
                      ?? new ProfileCacheOptions();

    // Enhanced logging for debugging
    logger.LogInformation("Environment: {Environment}, IsDevelopment: {IsDevelopment}", 
        builder.Environment.EnvironmentName, builder.Environment.IsDevelopment());
    
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
    logger.LogInformation("Redis connection string configured: {HasRedis}, Value: {RedisConnectionString}", 
        !string.IsNullOrEmpty(redisConnectionString), 
        string.IsNullOrEmpty(redisConnectionString) ? "NOT SET" : redisConnectionString);
    
    // Use Redis if connection string is provided, regardless of environment
    if (!string.IsNullOrEmpty(redisConnectionString))
    {
      try
      {
        logger.LogInformation("Attempting to connect to Redis at: {RedisConnectionString}", redisConnectionString);
        
        // Add StackExchange Redis for L2 cache
        // Use async connect to correctly handle Redis Sentinel topology resolution
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
          var configOptions = ConfigurationOptions.Parse(redisConnectionString);

          // Harden for Sentinel environments
          configOptions.AbortOnConnectFail = false;
          configOptions.ConnectRetry = 5;
          configOptions.ConnectTimeout = 10000;    // 10 s – Sentinel needs time to resolve master
          configOptions.SyncTimeout = 5000;
          configOptions.ReconnectRetryPolicy = new ExponentialRetry(3000);

          logger.LogInformation(
              "Connecting to Redis – ServiceName (Sentinel): {ServiceName}, Endpoints: {Endpoints}",
              configOptions.ServiceName ?? "(none)",
              string.Join(", ", configOptions.EndPoints.Select(ep => ep.ToString())));

          // ConnectAsync lets Sentinel finish master-discovery before returning
          var multiplexer = ConnectionMultiplexer.ConnectAsync(configOptions).GetAwaiter().GetResult();

          foreach (var server in multiplexer.GetServers())
          {
            logger.LogInformation(
                "Redis server {Endpoint} – IsConnected: {IsConnected}, IsReplica: {IsReplica}, ServerType: {ServerType}",
                server.EndPoint, server.IsConnected, server.IsReplica, server.ServerType);
          }

          var db = multiplexer.GetDatabase();
          logger.LogInformation(
              "Redis connection established successfully – Database: {Database}, Endpoints: {Endpoints}",
              db.Database, string.Join(", ", multiplexer.GetEndPoints().Select(ep => ep.ToString())));

          return multiplexer;
        });
        
        // Add StackExchange Redis Cache – reuse the hardened singleton multiplexer
        // so both caching and distributed locks share the same resilient connection
        services.AddStackExchangeRedisCache(options =>
        {
          options.ConnectionMultiplexerFactory = () =>
              Task.FromResult(services.BuildServiceProvider().GetRequiredService<IConnectionMultiplexer>());
          options.InstanceName = "ApplicantPortal";
        });
        
        logger.LogInformation("Redis distributed cache configured (Redis connection: {RedisConnectionString})", redisConnectionString);
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Failed to connect to Redis at {RedisConnectionString}. Falling back to memory-only cache", redisConnectionString);
        
        // Fallback to memory-only on Redis connection failure
        // DO NOT register IConnectionMultiplexer when Redis fails
        services.AddDistributedMemoryCache();
        
        logger.LogWarning("Using fallback DistributedMemoryCache due to Redis connection failure");
      }
    }
    else
    {
      // No Redis connection string - use in-memory only
      // DO NOT register IConnectionMultiplexer when Redis is not configured
      services.AddDistributedMemoryCache();
      
      logger.LogInformation("Using DistributedMemoryCache (Redis not configured). Expiry: {CacheExpiry}min", 
          cacheConfig.CacheExpiryMinutes);
    }

    logger.LogInformation("Distributed cache configuration completed");
    
    return services;
  }
}
