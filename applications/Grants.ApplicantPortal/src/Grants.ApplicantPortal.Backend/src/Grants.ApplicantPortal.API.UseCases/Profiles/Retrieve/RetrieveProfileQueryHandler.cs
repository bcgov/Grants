using Grants.ApplicantPortal.API.Core.Features.Profiles.ProfileAggregate;
using Grants.ApplicantPortal.API.Core.Plugins;
using Microsoft.Extensions.Options;

namespace Grants.ApplicantPortal.API.UseCases.Profiles.Retrieve;

/// <summary>
/// Query handler for retrieving cached profile data for a specific plugin with automatic hydration
/// and built-in cache stampede protection using .NET 9 HybridCache (L1/L2 caching).
/// 
/// HybridCache provides automatic L1 (in-memory) and L2 (distributed) caching with built-in
/// serialization, stampede protection, and optimized performance.
/// </summary>
public class RetrieveProfileQueryHandler(
    HybridCache hybridCache,
    IProfilePluginFactory pluginFactory,
    IReadRepository<Profile> profileRepository,
    IOptions<ProfileCacheOptions> profileCacheOptions,
    ILogger<RetrieveProfileQueryHandler> logger)
    : IQueryHandler<RetrieveProfileQuery, Result<ProfileData>>
{  
  public async Task<Result<ProfileData>> Handle(RetrieveProfileQuery request, CancellationToken cancellationToken)
  {
    logger.LogInformation("Retrieving profile data for ProfileId: {ProfileId}, PluginId: {PluginId}, Provider: {Provider}, Key: {Key}",
        request.ProfileId, request.PluginId, request.Provider, request.Key);

    try
    {
      // First, validate that the ProfileId exists in the database -- excluded for now
      //var profile = await profileRepository.GetByIdAsync(request.ProfileId, cancellationToken);
      //if (profile == null)
      //{
      //  logger.LogWarning("Profile not found in database for ProfileId: {ProfileId}", request.ProfileId);
      //  return Result.NotFound($"Profile with ID {request.ProfileId} not found");
      //}

      var cacheKey = $"{profileCacheOptions.Value.CacheKeyPrefix}{request.ProfileId}:{request.PluginId}:{request.Provider}:{request.Key}";

      // Configure cache options
      var entryOptions = new HybridCacheEntryOptions
      {
        Expiration = TimeSpan.FromMinutes(profileCacheOptions.Value.CacheExpiryMinutes),
        LocalCacheExpiration = TimeSpan.FromMinutes(profileCacheOptions.Value.SlidingExpiryMinutes)
      };

      // Use HybridCache with built-in stampede protection and L1/L2 caching
      var profileData = await hybridCache.GetOrCreateAsync(
        cacheKey,
        async cancel => await HydrateProfileDataAsync(request, cancel),
        entryOptions,
        null,
        cancellationToken);

      logger.LogInformation("Successfully retrieved profile data for ProfileId: {ProfileId}, PluginId: {PluginId}, Provider: {Provider}, Key: {Key}",
          request.ProfileId, request.PluginId, request.Provider, request.Key);

      return Result.Success(profileData);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error retrieving profile data for ProfileId: {ProfileId}, PluginId: {PluginId}, Provider: {Provider}, Key: {Key}",
          request.ProfileId, request.PluginId, request.Provider, request.Key);
      return Result.Error($"Failed to retrieve profile data: {ex.Message}");
    }
  }

  /// <summary>
  /// Hydrates profile data from the plugin. This method is called by HybridCache
  /// when the data is not available in L1 or L2 cache.
  /// </summary>
  private async Task<ProfileData> HydrateProfileDataAsync(RetrieveProfileQuery request, CancellationToken cancellationToken)
  {
    logger.LogInformation("Cache miss - hydrating profile data for ProfileId: {ProfileId}, PluginId: {PluginId}, Provider: {Provider}, Key: {Key}",
        request.ProfileId, request.PluginId, request.Provider, request.Key);

    // Get the appropriate plugin
    var plugin = pluginFactory.GetPlugin(request.PluginId);
    if (plugin == null)
    {
      logger.LogError("No plugin found for plugin ID: {PluginId}", request.PluginId);
      throw new InvalidOperationException($"No plugin found for plugin ID: {request.PluginId}");
    }

    // Create metadata for the plugin
    var metadata = new ProfilePopulationMetadata(
        request.ProfileId,
        request.PluginId,
        request.Provider,
        request.Key,
        request.AdditionalData);

    // Validate plugin can handle this request
    if (!plugin.CanHandle(metadata))
    {
      logger.LogWarning("Plugin {PluginId} cannot handle the provided metadata", request.PluginId);
      throw new InvalidOperationException($"Plugin {request.PluginId} cannot handle the provided metadata");
    }

    // Hydrate profile data using the plugin
    var profileData = await plugin.PopulateProfileAsync(metadata, cancellationToken);

    logger.LogInformation("Profile data hydrated successfully for ProfileId: {ProfileId}, PluginId: {PluginId}, Provider: {Provider}, Key: {Key}",
        request.ProfileId, request.PluginId, request.Provider, request.Key);

    return profileData;
  }
}
