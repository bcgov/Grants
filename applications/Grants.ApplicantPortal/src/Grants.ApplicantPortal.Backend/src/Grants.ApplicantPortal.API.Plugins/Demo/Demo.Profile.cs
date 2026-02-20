using System.Text.Json;
using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Messages;
using Microsoft.Extensions.Caching.Distributed;

namespace Grants.ApplicantPortal.API.Plugins.Demo;

/// <summary>
/// Profile population functionality for Demo plugin
/// </summary>
public partial class DemoPlugin
{
  public async Task<ProfileData> PopulateProfileAsync(ProfilePopulationMetadata metadata, CancellationToken cancellationToken = default)
  {
    logger.LogInformation("Demo plugin retrieving profile data for ProfileId: {ProfileId}, Provider: {Provider}, Key: {Key}",
        metadata.ProfileId, metadata.Provider, metadata.Key);

    try
    {
      // FIRST: Ensure demo data is seeded for this user profile (on-demand seeding)
      await SeedDataForProfileAsync(metadata.ProfileId, cancellationToken);

      // Simulate some processing time
      await Task.Delay(5, cancellationToken);

      var cacheKey = $"{_cacheOptions.Value.CacheKeyPrefix}{metadata.ProfileId}:DEMO:{metadata.Provider}:{metadata.Key}";

      logger.LogDebug("Looking for cached DEMO data with key: {CacheKey}", cacheKey);

      // ALWAYS fetch from Redis first - this is our persistent "database"
      var cachedBytes = await distributedCache.GetAsync(cacheKey, cancellationToken);
      if (cachedBytes != null)
      {
        var cachedProfileData = JsonSerializer.Deserialize<ProfileData>(cachedBytes, _jsonOptions);
        if (cachedProfileData != null)
        {
          cachedProfileData.CacheStatus = "HIT";
          cachedProfileData.CacheStore = _cacheStoreType;

          logger.LogInformation("Demo plugin successfully retrieved cached profile data for ProfileId: {ProfileId}, Provider: {Provider}, Key: {Key}",
              metadata.ProfileId, metadata.Provider, metadata.Key);
          
          return cachedProfileData;
        }
      }

      // Cache miss after seeding attempt - this shouldn't happen often
      // Generate fresh data but persist it with long expiration
      logger.LogWarning("DEMO data not found in Redis after seeding attempt for ProfileId: {ProfileId}, Provider: {Provider}, Key: {Key}. Generating and persisting fresh data.",
          metadata.ProfileId, metadata.Provider, metadata.Key);

      // Generate fresh data for this profile
      var mockProfileData = GenerateSeedingMockData(metadata);

      var profileData = new ProfileData(
          metadata.ProfileId,
          metadata.PluginId,
          metadata.Provider,
          metadata.Key,
          mockProfileData)
      {
          CacheStatus = "MISS",
          CacheStore = _cacheStoreType
      };

      // Store in cache using configured expiration settings
      var profileDataBytes = JsonSerializer.SerializeToUtf8Bytes(profileData, _jsonOptions);
      var cacheEntryOptions = new DistributedCacheEntryOptions
      {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_cacheOptions.Value.CacheExpiryMinutes),
        SlidingExpiration = TimeSpan.FromMinutes(_cacheOptions.Value.SlidingExpiryMinutes)
      };
      await distributedCache.SetAsync(cacheKey, profileDataBytes, cacheEntryOptions, cancellationToken);

      logger.LogInformation("Demo plugin successfully generated and persisted fresh profile data for ProfileId: {ProfileId}, Provider: {Provider}, Key: {Key}",
          metadata.ProfileId, metadata.Provider, metadata.Key);

      // Fire a message when profile data is populated
      await FireProfileUpdatedMessage(metadata, cancellationToken);

      return profileData;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Demo plugin failed to retrieve profile data for ProfileId: {ProfileId}, Provider: {Provider}, Key: {Key}",
          metadata.ProfileId, metadata.Provider, metadata.Key);
      throw;
    }
  }

  /// <summary>
  /// Helper method to fire profile updated message
  /// </summary>
  private async Task FireProfileUpdatedMessage(ProfilePopulationMetadata metadata, CancellationToken cancellationToken)
  {
    if (messagePublisher == null)
    {
      logger.LogDebug("Message publisher not available - skipping ProfileUpdatedMessage");
      return;
    }

    try
    {
      var message = new ProfileUpdatedMessage(
          metadata.ProfileId,
          PluginId,
          metadata.Provider,
          metadata.Key,
          correlationId: $"profile-{metadata.ProfileId}");

      await messagePublisher.PublishAsync(message, cancellationToken);

      logger.LogDebug("Published ProfileUpdatedMessage for {ProfileId}, Provider: {Provider}, Key: {Key}",
          metadata.ProfileId, metadata.Provider, metadata.Key);
    }
    catch (Exception ex)
    {
      logger.LogWarning(ex, "Failed to publish ProfileUpdatedMessage for {ProfileId}", metadata.ProfileId);
      // Don't throw - messaging failures shouldn't break the main operation
    }
  }
}
