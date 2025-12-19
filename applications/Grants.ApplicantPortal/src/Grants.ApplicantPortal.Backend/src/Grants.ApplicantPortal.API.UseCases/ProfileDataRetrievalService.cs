using Grants.ApplicantPortal.API.Core.Features.Profiles.ProfileAggregate;
using Grants.ApplicantPortal.API.Core.Plugins;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Grants.ApplicantPortal.API.UseCases;

/// <summary>
/// Shared service for retrieving profile data with caching and automatic hydration.
/// This service encapsulates the common logic for all profile data retrieval operations.
/// </summary>
public interface IProfileDataRetrievalService
{
  /// <summary>
  /// Retrieves profile data for a specific key (CONTACTS, SUBMISSIONS, ADDRESSES, ORGINFO, etc.)
  /// with automatic cache hydration and stampede protection.
  /// </summary>
  Task<Result<ProfileData>> RetrieveProfileDataAsync(
    Guid profileId,
    string pluginId,
    string provider,
    string key,
    Dictionary<string, object>? additionalData = null,
    CancellationToken cancellationToken = default);
}

/// <summary>
/// Shared implementation for profile data retrieval with caching and automatic hydration.
/// This service provides the common logic used by all specific profile data handlers.
/// </summary>
public class ProfileDataRetrievalService(
IDistributedCache distributedCache,
IProfilePluginFactory pluginFactory,
IReadRepository<Profile> profileRepository,
IOptions<ProfileCacheOptions> profileCacheOptions,
ILogger<ProfileDataRetrievalService> logger) : IProfileDataRetrievalService
{
  public async Task<Result<ProfileData>> RetrieveProfileDataAsync(
    Guid profileId,
    string pluginId,
    string provider,
    string key,
    Dictionary<string, object>? additionalData = null,
    CancellationToken cancellationToken = default)
  {
    logger.LogInformation("Retrieving {DataType} data for ProfileId: {ProfileId}, PluginId: {PluginId}, Provider: {Provider}",
        key, profileId, pluginId, provider);

    try
    {
      // First, validate that the ProfileId exists in the database -- excluded for now
      //var profile = await profileRepository.GetByIdAsync(profileId, cancellationToken);
      //if (profile == null)
      //{
      //  logger.LogWarning("Profile not found in database for ProfileId: {ProfileId}", profileId);
      //  return Result.NotFound($"Profile with ID {profileId} not found");
      //}

      var cacheKey = $"{profileCacheOptions.Value.CacheKeyPrefix}{profileId}:{pluginId}:{provider}:{key}";

      logger.LogInformation("Cache key: {CacheKey} for {DataType} data", cacheKey, key);

      // Configure cache options for distributed cache
      var cacheExpiryMinutes = profileCacheOptions.Value.CacheExpiryMinutes;
      var slidingExpiryMinutes = profileCacheOptions.Value.SlidingExpiryMinutes;

      logger.LogInformation("Cache options: Expiration={Expiration}min, SlidingExpiration={SlidingExpiration}min", 
          cacheExpiryMinutes, slidingExpiryMinutes);

      // Use IDistributedCache directly for reliable Redis caching
      try
      {
        // First try to get from cache (seeded DEMO data or previously cached data)
        var cachedBytes = await distributedCache.GetAsync(cacheKey, cancellationToken);
        if (cachedBytes != null)
        {
          logger.LogInformation("Cache hit for key: {CacheKey}", cacheKey);
          var cachedProfileData = JsonSerializer.Deserialize<ProfileData>(cachedBytes);
          if (cachedProfileData != null)
          {
            logger.LogInformation("Successfully retrieved {DataType} data for ProfileId: {ProfileId}, PluginId: {PluginId}, Provider: {Provider}",
                key, profileId, pluginId, provider);
            return Result.Success(cachedProfileData);
          }
        }

        logger.LogInformation("Cache miss - hydrating {Provider} data for ProfileId: {ProfileId}, PluginId: {PluginId}", provider, profileId, pluginId);

        // Cache miss - get data from plugin
        var profileData = await HydrateProfileDataAsync(profileId, pluginId, provider, key, additionalData, cancellationToken);

        // Store in distributed cache for future requests
        var dataBytes = JsonSerializer.SerializeToUtf8Bytes(profileData);
        var cacheOptions = new DistributedCacheEntryOptions
        {
          AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheExpiryMinutes),
          SlidingExpiration = TimeSpan.FromMinutes(slidingExpiryMinutes)
        };
        
        await distributedCache.SetAsync(cacheKey, dataBytes, cacheOptions, cancellationToken);
        logger.LogInformation("Cached {Provider} data for ProfileId: {ProfileId}", provider, profileId);

        logger.LogInformation("Cache operation completed for key: {CacheKey}", cacheKey);
        logger.LogInformation("Successfully retrieved {DataType} data for ProfileId: {ProfileId}, PluginId: {PluginId}, Provider: {Provider}",
            key, profileId, pluginId, provider);

        return Result.Success(profileData);
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error during cache operations for key: {CacheKey}", cacheKey);
        // Fallback to direct plugin call without caching
        var fallbackData = await HydrateProfileDataAsync(profileId, pluginId, provider, key, additionalData, cancellationToken);
        return Result.Success(fallbackData);
      }
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error retrieving {DataType} data for ProfileId: {ProfileId}, PluginId: {PluginId}, Provider: {Provider}",
          key, profileId, pluginId, provider);
      return Result.Error($"Failed to retrieve {key.ToLowerInvariant()} data: {ex.Message}");
    }
  }

  /// <summary>
  /// Hydrates profile data from the plugin. This method is called by HybridCache
  /// when the data is not available in L1 or L2 cache.
  /// </summary>
  private async Task<ProfileData> HydrateProfileDataAsync(
    Guid profileId,
    string pluginId,
    string provider,
    string key,
    Dictionary<string, object>? additionalData,
    CancellationToken cancellationToken)
  {
    logger.LogInformation("Cache miss - hydrating {DataType} data for ProfileId: {ProfileId}, PluginId: {PluginId}, Provider: {Provider}",
        key, profileId, pluginId, provider);

    // Get the appropriate plugin
    var plugin = pluginFactory.GetPlugin(pluginId);
    if (plugin == null)
    {
      logger.LogError("No plugin found for plugin ID: {PluginId}", pluginId);
      throw new InvalidOperationException($"No plugin found for plugin ID: {pluginId}");
    }

    // Create metadata for the plugin
    var metadata = new ProfilePopulationMetadata(
        profileId,
        pluginId,
        provider,
        key,
        additionalData);

    // Validate plugin can handle this request
    if (!plugin.CanHandle(metadata))
    {
      logger.LogWarning("Plugin {PluginId} cannot handle the provided metadata for {DataType}", pluginId, key);
      throw new InvalidOperationException($"Plugin {pluginId} cannot handle the provided metadata for {key.ToLowerInvariant()}");
    }

    // Hydrate profile data using the plugin
    var profileData = await plugin.PopulateProfileAsync(metadata, cancellationToken);

    logger.LogInformation("{DataType} data hydrated successfully for ProfileId: {ProfileId}, PluginId: {PluginId}, Provider: {Provider}",
        key, profileId, pluginId, provider);

    return profileData;
  }
}
