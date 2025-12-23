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
    _logger.LogInformation("Demo plugin retrieving profile data for ProfileId: {ProfileId}, Provider: {Provider}, Key: {Key}",
        metadata.ProfileId, metadata.Provider, metadata.Key);

    try
    {
      // FIRST: Ensure demo data is seeded for this user profile (on-demand seeding)
      await SeedDataForProfileAsync(metadata.ProfileId, cancellationToken);

      // Simulate some processing time
      await Task.Delay(5, cancellationToken);

      var cacheKey = $"{_cacheOptions.Value.CacheKeyPrefix}{metadata.ProfileId}:DEMO:{metadata.Provider}:{metadata.Key}";

      _logger.LogDebug("Looking for cached DEMO data with key: {CacheKey}", cacheKey);

      // ALWAYS fetch from Redis first - this is our persistent "database"
      var cachedBytes = await _distributedCache.GetAsync(cacheKey, cancellationToken);
      if (cachedBytes != null)
      {
        var cachedProfileData = JsonSerializer.Deserialize<ProfileData>(cachedBytes);
        if (cachedProfileData != null)
        {
          _logger.LogInformation("Demo plugin successfully retrieved cached profile data for ProfileId: {ProfileId}, Provider: {Provider}, Key: {Key}",
              metadata.ProfileId, metadata.Provider, metadata.Key);
          
          return cachedProfileData;
        }
      }

      // Cache miss after seeding attempt - this shouldn't happen often
      // Generate fresh data but persist it with long expiration
      _logger.LogWarning("DEMO data not found in Redis after seeding attempt for ProfileId: {ProfileId}, Provider: {Provider}, Key: {Key}. Generating and persisting fresh data.",
          metadata.ProfileId, metadata.Provider, metadata.Key);

      // Generate fresh data for this profile
      var mockProfileData = GenerateSeedingMockData(metadata);

      var jsonData = JsonSerializer.Serialize(mockProfileData, new JsonSerializerOptions
      {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
      });

      var profileData = new ProfileData(
          metadata.ProfileId,
          metadata.PluginId,
          metadata.Provider,
          metadata.Key,
          jsonData);

      // Store in Redis with long-term expiration
      var profileDataBytes = JsonSerializer.SerializeToUtf8Bytes(profileData);
      var longTermCacheOptions = new DistributedCacheEntryOptions
      {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(365)
      };
      await _distributedCache.SetAsync(cacheKey, profileDataBytes, longTermCacheOptions, cancellationToken);

      _logger.LogInformation("Demo plugin successfully generated and persisted fresh profile data for ProfileId: {ProfileId}, Provider: {Provider}, Key: {Key}",
          metadata.ProfileId, metadata.Provider, metadata.Key);

      // Fire a message when profile data is populated
      await FireProfileUpdatedMessage(metadata, cancellationToken);

      return profileData;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Demo plugin failed to retrieve profile data for ProfileId: {ProfileId}, Provider: {Provider}, Key: {Key}",
          metadata.ProfileId, metadata.Provider, metadata.Key);
      throw;
    }
  }

  /// <summary>
  /// Helper method to fire profile updated message
  /// </summary>
  private async Task FireProfileUpdatedMessage(ProfilePopulationMetadata metadata, CancellationToken cancellationToken)
  {
    if (_messagePublisher == null)
    {
      _logger.LogDebug("Message publisher not available - skipping ProfileUpdatedMessage");
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

      await _messagePublisher.PublishAsync(message, cancellationToken);

      _logger.LogDebug("Published ProfileUpdatedMessage for {ProfileId}, Provider: {Provider}, Key: {Key}",
          metadata.ProfileId, metadata.Provider, metadata.Key);
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "Failed to publish ProfileUpdatedMessage for {ProfileId}", metadata.ProfileId);
      // Don't throw - messaging failures shouldn't break the main operation
    }
  }
}
