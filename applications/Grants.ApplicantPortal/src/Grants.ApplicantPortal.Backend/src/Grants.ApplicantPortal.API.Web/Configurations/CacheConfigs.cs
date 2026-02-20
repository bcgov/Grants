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
        services.AddSingleton<IConnectionMultiplexer>(provider =>
        {
          var configOptions = ConfigurationOptions.Parse(redisConnectionString);
          var multiplexer = ConnectionMultiplexer.Connect(configOptions);
          
          // Get the database number from connection string or use default 0
          var database = multiplexer.GetDatabase();
          logger.LogInformation("Redis connection established successfully - Database: {Database}, Endpoints: {Endpoints}", 
              database.Database, string.Join(", ", multiplexer.GetEndPoints().Select(ep => ep.ToString())));
          return multiplexer;
        });
        
        // Add StackExchange Redis Cache for distributed caching (Redis only, no HybridCache)
        services.AddStackExchangeRedisCache(options =>
        {
          options.Configuration = redisConnectionString;
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
