using Grants.ApplicantPortal.API.Core.Features.Profiles.ProfileAggregate;
using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.UseCases;

/// <summary>
/// Shared service for retrieving profile data.
/// Validates the profile, resolves the plugin, and delegates to the plugin's
/// <see cref="IProfilePlugin.PopulateProfileAsync"/> method.
/// Caching is an implementation detail of each plugin.
/// </summary>
public interface IProfileDataRetrievalService
{
  Task<Result<ProfileData>> RetrieveProfileDataAsync(
    Guid profileId,
    string pluginId,
    string provider,
    string key,
    string subject,
    Dictionary<string, object>? additionalData = null,
    CancellationToken cancellationToken = default);
}

public class ProfileDataRetrievalService(
    IProfilePluginFactory pluginFactory,
    IReadRepository<Profile> profileRepository,
    ILogger<ProfileDataRetrievalService> logger) : IProfileDataRetrievalService
{
  public async Task<Result<ProfileData>> RetrieveProfileDataAsync(
    Guid profileId,
    string pluginId,
    string provider,
    string key,
    string subject,
    Dictionary<string, object>? additionalData = null,
    CancellationToken cancellationToken = default)
  {
    logger.LogInformation("Retrieving {DataType} data for ProfileId: {ProfileId}, PluginId: {PluginId}, Provider: {Provider}",
        key, profileId, pluginId, provider);

    try
    {
      var profile = await profileRepository.GetByIdAsync(profileId, cancellationToken);
      if (profile == null)
      {
        logger.LogWarning("Profile not found for ProfileId: {ProfileId}", profileId);
        return Result.NotFound($"Profile with ID {profileId} not found");
      }

      var plugin = pluginFactory.GetPlugin(pluginId);
      if (plugin == null)
      {
        logger.LogError("No plugin found for PluginId: {PluginId}", pluginId);
        return Result.Error($"No plugin found for plugin ID: {pluginId}");
      }

      var metadata = new ProfilePopulationMetadata(
          profileId, pluginId, provider, key, subject, additionalData);

      if (!plugin.CanHandle(metadata))
      {
        logger.LogWarning("Plugin {PluginId} cannot handle metadata for {DataType}", pluginId, key);
        return Result.Error($"Plugin {pluginId} cannot handle the provided metadata for {key.ToLowerInvariant()}");
      }

      var profileData = await plugin.PopulateProfileAsync(metadata, cancellationToken);

      logger.LogInformation("Successfully retrieved {DataType} data for ProfileId: {ProfileId}, PluginId: {PluginId}, Provider: {Provider}",
          key, profileId, pluginId, provider);

      return Result.Success(profileData);
    }
    catch (Exception ex)
    {
      // Log the full exception detail (message, plugin name, downstream status, ids) for
      // server-side diagnostics only. The returned Result must stay generic — it is relayed
      // verbatim to the HTTP client and must never leak internal plugin/infrastructure details.
      logger.LogError(ex, "Error retrieving {DataType} data for ProfileId: {ProfileId}, PluginId: {PluginId}, Provider: {Provider}",
          key, profileId, pluginId, provider);
      return Result.Error("Unable to retrieve the requested data. Please try again later.");
    }
  }
}
