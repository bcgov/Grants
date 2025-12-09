using Grants.ApplicantPortal.API.Core.Features.Profiles.ProfileAggregate;
using Grants.ApplicantPortal.API.Core.Plugins;
using Microsoft.Extensions.Options;

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
    HybridCache hybridCache,
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

      // Configure cache options
      var entryOptions = new HybridCacheEntryOptions
      {
        Expiration = TimeSpan.FromMinutes(profileCacheOptions.Value.CacheExpiryMinutes),
        LocalCacheExpiration = TimeSpan.FromMinutes(profileCacheOptions.Value.SlidingExpiryMinutes)
      };

      // Use HybridCache with built-in stampede protection and L1/L2 caching
      var profileData = await hybridCache.GetOrCreateAsync(
        cacheKey,
        async cancel => await HydrateProfileDataAsync(profileId, pluginId, provider, key, additionalData, cancel),
        entryOptions,
        null,
        cancellationToken);

      logger.LogInformation("Successfully retrieved {DataType} data for ProfileId: {ProfileId}, PluginId: {PluginId}, Provider: {Provider}",
          key, profileId, pluginId, provider);

      return Result.Success(profileData);
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
