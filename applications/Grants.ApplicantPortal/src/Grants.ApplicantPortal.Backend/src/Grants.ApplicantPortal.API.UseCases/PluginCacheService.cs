using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Grants.ApplicantPortal.API.UseCases;

/// <summary>
/// Shared cache-aside service for plugin data.
/// Provides a consistent pattern for cache-check → fetch → store across all plugins.
/// The result type is fully generic — callers decide what to cache and when.
/// Cache keys follow the project convention: <c>{prefix}{profileId}:{pluginId}:{segment}</c>
/// so keys are unambiguous across plugins by default.
/// </summary>
public interface IPluginCacheService
{
    /// <summary>
    /// Gets data from cache or calls the factory on a miss.
    /// An optional <paramref name="shouldCache"/> predicate controls whether the
    /// fetched result is stored. When omitted the result is always cached.
    /// </summary>
    /// <typeparam name="T">The result type (list, single object, record, etc.).</typeparam>
    /// <param name="profileId">The profile that owns the cached data.</param>
    /// <param name="pluginId">The plugin producing the data (e.g. "UNITY", "DEMO").</param>
    /// <param name="cacheSegment">
    /// The trailing portion of the cache key (e.g. "providers", "contacts").
    /// The full key is built as <c>{CacheKeyPrefix}{profileId}:{pluginId}:{cacheSegment}</c>.
    /// </param>
    /// <param name="factory">Async factory invoked on a cache miss to produce the data.</param>
    /// <param name="shouldCache">
    /// Optional predicate evaluated against the factory result.
    /// Return <c>true</c> to cache, <c>false</c> to skip (e.g. empty list, null-ish state).
    /// When <c>null</c> the result is always cached.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<T> GetOrFetchAsync<T>(
        Guid profileId,
        string pluginId,
        string cacheSegment,
        Func<CancellationToken, Task<T>> factory,
        Func<T, bool>? shouldCache = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Builds a consistent cache key for a given profile, plugin, and data segment.
    /// Useful when callers need the key for manual operations.
    /// </summary>
    string BuildCacheKey(Guid profileId, string pluginId, string segment);

    /// <summary>
    /// Removes a cached entry for a given profile, plugin, and data segment.
    /// </summary>
    Task InvalidateAsync(Guid profileId, string pluginId, string cacheSegment, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of the shared plugin cache-aside service.
/// Uses <see cref="IDistributedCache"/> (Redis or in-memory) with
/// expiration settings from <see cref="ProfileCacheOptions"/>.
/// Keys follow the convention: <c>{prefix}{profileId}:{pluginId}:{segment}</c>.
/// </summary>
public class PluginCacheService(
    IDistributedCache distributedCache,
    IOptions<ProfileCacheOptions> cacheOptions,
    ILogger<PluginCacheService> logger) : IPluginCacheService
{
    public async Task<T> GetOrFetchAsync<T>(
        Guid profileId,
        string pluginId,
        string cacheSegment,
        Func<CancellationToken, Task<T>> factory,
        Func<T, bool>? shouldCache = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildCacheKey(profileId, pluginId, cacheSegment);

        // 1. Try cache
        var cached = await distributedCache.GetStringAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            try
            {
                var deserialized = JsonSerializer.Deserialize<T>(cached);
                if (deserialized is not null)
                {
                    logger.LogDebug("Cache hit for {PluginId}:{Segment}, ProfileId: {ProfileId}",
                        pluginId, cacheSegment, profileId);
                    return deserialized;
                }

                logger.LogWarning("Cache entry deserialized to null for {PluginId}:{Segment}, ProfileId: {ProfileId} — removing and falling through to factory",
                    pluginId, cacheSegment, profileId);
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Corrupt cache entry for {PluginId}:{Segment}, ProfileId: {ProfileId} — removing and falling through to factory",
                    pluginId, cacheSegment, profileId);
            }

            await distributedCache.RemoveAsync(cacheKey, cancellationToken);
        }

        // 2. Cache miss — invoke factory
        logger.LogInformation("Cache miss for {PluginId}:{Segment}, ProfileId: {ProfileId}",
            pluginId, cacheSegment, profileId);

        var result = await factory(cancellationToken);

        // 3. Evaluate caching predicate (default: always cache)
        if (shouldCache is null || shouldCache(result))
        {
            var json = JsonSerializer.Serialize(result);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheOptions.Value.CacheExpiryMinutes),
                SlidingExpiration = TimeSpan.FromMinutes(cacheOptions.Value.SlidingExpiryMinutes)
            };
            await distributedCache.SetStringAsync(cacheKey, json, options, cancellationToken);
            logger.LogDebug("Cached result for {PluginId}:{Segment}, ProfileId: {ProfileId}",
                pluginId, cacheSegment, profileId);
        }
        else
        {
            logger.LogInformation("Skipping cache for {PluginId}:{Segment}, ProfileId: {ProfileId} — shouldCache returned false",
                pluginId, cacheSegment, profileId);
        }

        return result;
    }

    public string BuildCacheKey(Guid profileId, string pluginId, string segment)
        => $"{cacheOptions.Value.CacheKeyPrefix}{profileId}:{pluginId}:{segment}";

    public async Task InvalidateAsync(Guid profileId, string pluginId, string cacheSegment, CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildCacheKey(profileId, pluginId, cacheSegment);

        logger.LogInformation("Invalidating cache for {PluginId}:{Segment}, ProfileId: {ProfileId}",
            pluginId, cacheSegment, profileId);

        try
        {
            await distributedCache.RemoveAsync(cacheKey, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to invalidate cache for {PluginId}:{Segment}, ProfileId: {ProfileId}",
                pluginId, cacheSegment, profileId);
        }
    }
}
