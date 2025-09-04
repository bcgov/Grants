using Grants.ApplicantPortal.API.UseCases.Profiles;
using Microsoft.Extensions.Caching.Hybrid;
using StackExchange.Redis;

namespace Grants.ApplicantPortal.API.Web.Configurations;

public static class CacheConfigs
{
  public static IServiceCollection AddCacheConfigs(this IServiceCollection services, WebApplicationBuilder builder, Microsoft.Extensions.Logging.ILogger logger)
  {
    // Add .NET 9 HybridCache for L1/L2 caching with automatic stampede protection
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
    
    if (builder.Environment.IsDevelopment())
    {
      // Development: Use HybridCache with in-memory only (no Redis needed for dev)
      services.AddHybridCache(options =>
      {
        options.DefaultEntryOptions = new HybridCacheEntryOptions
        {
          Expiration = TimeSpan.FromMinutes(cacheConfig.CacheExpiryMinutes), // From appsettings
          LocalCacheExpiration = TimeSpan.FromMinutes(cacheConfig.SlidingExpiryMinutes) // From appsettings
        };
      });
      
      logger.LogInformation("Using HybridCache with in-memory L1 cache for development (Expiry: {CacheExpiry}min, LocalExpiry: {LocalExpiry}min)", 
          cacheConfig.CacheExpiryMinutes, cacheConfig.SlidingExpiryMinutes);
    }
    else
    {
      // Production: Use HybridCache with Redis as L2 cache
      logger.LogInformation("Production environment detected - configuring Redis L2 cache");
      
      if (!string.IsNullOrEmpty(redisConnectionString))
      {
        try
        {
          logger.LogInformation("Attempting to connect to Redis at: {RedisConnectionString}", redisConnectionString);
          
          // Add StackExchange Redis for L2 cache
          services.AddSingleton<IConnectionMultiplexer>(provider =>
          {
            var multiplexer = ConnectionMultiplexer.Connect(redisConnectionString);
            logger.LogInformation("Redis connection established successfully");
            return multiplexer;
          });
          
          services.AddStackExchangeRedisCache(options =>
          {
            options.Configuration = redisConnectionString;
            options.InstanceName = "ApplicantPortal";
          });

          // Configure HybridCache with Redis as L2
          services.AddHybridCache(options =>
          {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
              Expiration = TimeSpan.FromMinutes(cacheConfig.CacheExpiryMinutes), // L2 (Redis) expiration from appsettings
              LocalCacheExpiration = TimeSpan.FromMinutes(Math.Min(cacheConfig.SlidingExpiryMinutes, cacheConfig.CacheExpiryMinutes / 6)) // L1 shorter for production
            };
          });
          
          logger.LogInformation("HybridCache configured with Redis L2 cache for production (L2 Expiry: {L2Expiry}min, L1 Expiry: {L1Expiry}min)", 
              cacheConfig.CacheExpiryMinutes, Math.Min(cacheConfig.SlidingExpiryMinutes, cacheConfig.CacheExpiryMinutes / 6));
        }
        catch (Exception ex)
        {
          logger.LogError(ex, "Failed to connect to Redis at {RedisConnectionString}. Falling back to memory-only cache", redisConnectionString);
          
          // Fallback to memory-only on Redis connection failure
          services.AddHybridCache(options =>
          {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
              Expiration = TimeSpan.FromMinutes(cacheConfig.CacheExpiryMinutes),
              LocalCacheExpiration = TimeSpan.FromMinutes(cacheConfig.SlidingExpiryMinutes)
            };
          });
          
          logger.LogWarning("Using fallback HybridCache with memory only due to Redis connection failure");
        }
      }
      else
      {
        // Fallback: HybridCache with memory only
        services.AddHybridCache(options =>
        {
          options.DefaultEntryOptions = new HybridCacheEntryOptions
          {
            Expiration = TimeSpan.FromMinutes(cacheConfig.CacheExpiryMinutes),
            LocalCacheExpiration = TimeSpan.FromMinutes(cacheConfig.SlidingExpiryMinutes)
          };
        });
        
        logger.LogWarning("Redis not configured - using HybridCache with memory only (not optimal for multi-pod). Expiry: {CacheExpiry}min, LocalExpiry: {LocalExpiry}min", 
            cacheConfig.CacheExpiryMinutes, cacheConfig.SlidingExpiryMinutes);
      }
    }

    logger.LogInformation("HybridCache L1/L2 caching configuration completed");
    
    return services;
  }
}
