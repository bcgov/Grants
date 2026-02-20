using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Distributed;

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
      string key,
      CancellationToken cancellationToken);
}

/// <summary>
/// Implementation of profile cache invalidation service
/// </summary>
public class ProfileCacheInvalidationService(
    IDistributedCache distributedCache,
    IOptions<ProfileCacheOptions> profileCacheOptions,
    ILogger<ProfileCacheInvalidationService> logger) : IProfileCacheInvalidationService
{
  public async Task InvalidateProfileDataCacheAsync(
      Guid profileId,
      string pluginId,
      string provider,
      string key,
      CancellationToken cancellationToken)
  {
    try
    {
      var cacheKey = $"{profileCacheOptions.Value.CacheKeyPrefix}{profileId}:{pluginId}:{provider}:{key}";

      logger.LogInformation("Invalidating cache for key: {CacheKey} (ProfileId: {ProfileId}, DataType: {DataType})",
          cacheKey, profileId, key);

      await distributedCache.RemoveAsync(cacheKey, cancellationToken);

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
}
