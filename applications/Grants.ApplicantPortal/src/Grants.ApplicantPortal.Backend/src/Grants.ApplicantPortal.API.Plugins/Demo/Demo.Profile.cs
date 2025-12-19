using System.Text.Json;
using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Messages;

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
      // Simulate some processing time
      await Task.Delay(5, cancellationToken); // Reduced since we're just reading from cache

      var cacheKey = $"{_cacheOptions.Value.CacheKeyPrefix}{metadata.ProfileId}:DEMO:{metadata.Provider}:{metadata.Key}";

      _logger.LogDebug("Looking for cached DEMO data with key: {CacheKey}", cacheKey);

      // IMPORTANT: Don't use GetOrCreateAsync here to avoid infinite loop!
      // The ProfileDataRetrievalService handles the GetOrCreateAsync logic
      // This method should only generate data when explicitly called

      // If not in cache, this profile ID wasn't seeded - generate on demand but warn
      _logger.LogWarning("Generating on-demand DEMO data for ProfileId: {ProfileId}, Provider: {Provider}, Key: {Key}. This should have been seeded on startup.",
          metadata.ProfileId, metadata.Provider, metadata.Key);

      // Generate on-demand for non-seeded profile IDs
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

      _logger.LogInformation("Demo plugin successfully generated on-demand profile data for ProfileId: {ProfileId}, Provider: {Provider}, Key: {Key}",
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
