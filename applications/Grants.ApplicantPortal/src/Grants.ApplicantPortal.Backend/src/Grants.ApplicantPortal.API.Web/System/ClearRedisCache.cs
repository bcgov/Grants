using StackExchange.Redis;
using Microsoft.Extensions.Caching.Distributed;
using Grants.ApplicantPortal.API.UseCases;

namespace Grants.ApplicantPortal.API.Web.System;

/// <summary>
/// Diagnostic endpoint to clear Redis cache keys for testing
/// </summary>
public class ClearRedisCache : EndpointWithoutRequest<ClearRedisCacheResponse>
{
    private readonly IConnectionMultiplexer? _connectionMultiplexer;
    private readonly IDistributedCache _distributedCache;
    private readonly IProfileCacheInvalidationService _cacheInvalidationService;
    private readonly ILogger<ClearRedisCache> _logger;

    public ClearRedisCache(
        IDistributedCache distributedCache,
        IProfileCacheInvalidationService cacheInvalidationService,
        ILogger<ClearRedisCache> logger,
        IConnectionMultiplexer? connectionMultiplexer = null)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _distributedCache = distributedCache;
        _cacheInvalidationService = cacheInvalidationService;
        _logger = logger;
    }

    public override void Configure()
    {
        Post("/System/clear-redis-cache");
        AllowAnonymous();
        Summary(s => s.Summary = "Clear Redis cache keys for testing purposes");
        Tags("System", "Debug");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        _logger.LogInformation("=== Clearing Cache ===");

        var result = new ClearRedisCacheResponse
        {
            RedisConnected = _connectionMultiplexer != null && _connectionMultiplexer.IsConnected,
            KeysCleared = new List<string>(),
            KeyCount = 0,
            CacheInvalidationResults = new List<CacheInvalidationResult>()
        };

        if (_connectionMultiplexer != null && _connectionMultiplexer.IsConnected)
        {
            _logger.LogInformation("Using Redis for cache clearing");
            try
            {
                var database = _connectionMultiplexer.GetDatabase();
                var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
                
                _logger.LogInformation("Connected to Redis database {Database}", database.Database);

                // Get all profile cache keys with ApplicantPortal prefix
                var keys = server.Keys(database.Database, pattern: "ApplicantPortal*").ToList();
                result.KeyCount = keys.Count;

                _logger.LogInformation("Found {KeyCount} cache keys to clear", result.KeyCount);

                // Clear each key
                foreach (var key in keys)
                {
                    try
                    {
                        var keyString = key.ToString();
                        await database.KeyDeleteAsync(key);
                        result.KeysCleared.Add(keyString);
                        _logger.LogDebug("Cleared cache key: {Key}", keyString);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to clear cache key: {Key}", key.ToString());
                    }
                }

                result.Success = true;
                _logger.LogInformation("Successfully cleared {ClearedCount} out of {TotalCount} cache keys", 
                    result.KeysCleared.Count, result.KeyCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing Redis cache");
                result.ErrorMessage = ex.Message;
                result.Success = false;
            }
        }
        else
        {
            _logger.LogWarning("Redis not available - using distributed cache fallback (likely in-memory cache)");
            
            // For in-memory distributed cache, we can't enumerate keys, but we can test cache invalidation
            _logger.LogInformation("Testing cache invalidation service with in-memory distributed cache");
            
            result.Success = true;
            result.ErrorMessage = "Redis not connected - using in-memory distributed cache (cannot enumerate keys for clearing)";
        }

        // Test cache invalidation service regardless of Redis availability
        var testProfileId = Guid.NewGuid();
        var testScenarios = new[]
        {
            new { Provider = "PROGRAM1", Key = "CONTACTS", Description = "Program1 Contacts" },
            new { Provider = "PROGRAM1", Key = "ADDRESSES", Description = "Program1 Addresses" },
            new { Provider = "PROGRAM1", Key = "ORGINFO", Description = "Program1 Organization Info" }
        };

        foreach (var scenario in testScenarios)
        {
            var invalidationResult = new CacheInvalidationResult
            {
                Provider = scenario.Provider,
                Key = scenario.Key,
                Description = scenario.Description,
                ProfileId = testProfileId
            };

            try
            {
                await _cacheInvalidationService.InvalidateProfileDataCacheAsync(
                    testProfileId, "DEMO", scenario.Provider, scenario.Key, ct);
                
                invalidationResult.Success = true;
                _logger.LogInformation("Successfully tested cache invalidation for {Provider}:{Key}", 
                    scenario.Provider, scenario.Key);
            }
            catch (Exception ex)
            {
                invalidationResult.Success = false;
                invalidationResult.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Failed to test cache invalidation for {Provider}:{Key}", 
                    scenario.Provider, scenario.Key);
            }

            result.CacheInvalidationResults.Add(invalidationResult);
        }

        Response = result;
        await Task.CompletedTask;
    }
}

public class ClearRedisCacheResponse
{
    public bool Success { get; set; }
    public bool RedisConnected { get; set; }
    public List<string> KeysCleared { get; set; } = new();
    public int KeyCount { get; set; }
    public string? ErrorMessage { get; set; }
    public List<CacheInvalidationResult> CacheInvalidationResults { get; set; } = new();
}

public class CacheInvalidationResult
{
    public string Provider { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid ProfileId { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
