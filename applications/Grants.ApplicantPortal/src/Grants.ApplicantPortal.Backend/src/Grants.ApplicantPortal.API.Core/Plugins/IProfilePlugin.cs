namespace Grants.ApplicantPortal.API.Core.Plugins;

/// <summary>
/// Represents the metadata payload for profile population requests
/// </summary>
public record ProfilePopulationMetadata(
    Guid ProfileId,
    string PluginId,
    string Provider,
    string Key,
    Dictionary<string, object>? AdditionalData = null,
    DateTime RequestedAt = default
)
{
  public DateTime RequestedAt { get; init; } = RequestedAt == default ? DateTime.UtcNow : RequestedAt;
}

/// <summary>
/// Represents the profile data returned by plugins
/// </summary>
public record ProfileData(
    Guid ProfileId,
    string PluginId,
    string Provider,
    string Key,
    string JsonData,
    DateTime PopulatedAt = default
)
{
  public DateTime PopulatedAt { get; init; } = PopulatedAt == default ? DateTime.UtcNow : PopulatedAt;
}

/// <summary>
/// Base interface for profile population plugins
/// </summary>
public interface IProfilePlugin
{
  /// <summary>
  /// The unique identifier for this plugin
  /// </summary>
  string PluginId { get; }

  /// <summary>
  /// Populates profile data from external sources
  /// </summary>
  Task<ProfileData> PopulateProfileAsync(ProfilePopulationMetadata metadata, CancellationToken cancellationToken = default);

  /// <summary>
  /// Validates if the plugin can handle the given metadata
  /// </summary>
  bool CanHandle(ProfilePopulationMetadata metadata);
}

/// <summary>
/// Plugin factory for resolving plugins by plugin identifier
/// </summary>
public interface IProfilePluginFactory
{
  /// <summary>
  /// Gets a plugin by plugin identifier
  /// </summary>
  IProfilePlugin? GetPlugin(string pluginId);

  /// <summary>
  /// Gets all available plugins
  /// </summary>
  IEnumerable<IProfilePlugin> GetAllPlugins();
}
