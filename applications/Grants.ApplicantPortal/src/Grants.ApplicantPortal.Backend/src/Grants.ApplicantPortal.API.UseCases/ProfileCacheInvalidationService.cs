using Microsoft.Extensions.Options;

namespace Grants.ApplicantPortal.API.UseCases;

/// <summary>
/// Service for cache invalidation operations specific to profile data
/// </summary>
public interface IProfileCacheInvalidationService
{
    /// <summary>
    /// Invalidates cache for a specific profile data type (CONTACTS, ADDRESSES, etc.)
    /// </summary>
    Task InvalidateProfileDataCacheAsync(
        Guid profileId, 
        string pluginId, 
        string provider, 
        string key);
        
    /// <summary>
    /// Invalidates all cache entries for a specific profile
    /// </summary>
    Task InvalidateAllProfileCacheAsync(Guid profileId);
}

/// <summary>
/// Implementation of profile cache invalidation service
/// </summary>
public class ProfileCacheInvalidationService(
    HybridCache hybridCache,
    IOptions<ProfileCacheOptions> profileCacheOptions,
    ILogger<ProfileCacheInvalidationService> logger) : IProfileCacheInvalidationService
{
    public async Task InvalidateProfileDataCacheAsync(
        Guid profileId,
        string pluginId,
        string provider,
        string key)
    {
        try
        {
            var cacheKey = $"{profileCacheOptions.Value.CacheKeyPrefix}{profileId}:{pluginId}:{provider}:{key}";
            
            logger.LogInformation("Invalidating cache for key: {CacheKey} (ProfileId: {ProfileId}, DataType: {DataType})", 
                cacheKey, profileId, key);
                
            await hybridCache.RemoveAsync(cacheKey);
            
            logger.LogDebug("Successfully invalidated cache for ProfileId: {ProfileId}, DataType: {DataType}", 
                profileId, key);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to invalidate cache for ProfileId: {ProfileId}, DataType: {DataType}", 
                profileId, key);
            // Don't throw - cache invalidation failure shouldn't break the operation
        }
    }
    
    public async Task InvalidateAllProfileCacheAsync(Guid profileId)
    {
        try
        {
            logger.LogInformation("Invalidating all cache entries for ProfileId: {ProfileId}", profileId);
            
            // Note: HybridCache doesn't have a pattern-based removal method
            // For now, we'll just log this. In a real implementation, you might
            // need to track cache keys or use a different caching solution if
            // pattern-based invalidation is required.
            
            logger.LogWarning("Pattern-based cache invalidation not supported by HybridCache. " +
                            "Consider invalidating specific cache entries or implementing a different caching strategy.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to invalidate all cache entries for ProfileId: {ProfileId}", profileId);
        }
    }
}
