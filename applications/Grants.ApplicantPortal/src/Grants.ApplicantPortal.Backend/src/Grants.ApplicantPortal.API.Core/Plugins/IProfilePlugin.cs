namespace Grants.ApplicantPortal.API.Core.Plugins;

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
  /// Gets all supported features (provider/key combinations) for this plugin
  /// </summary>
  IReadOnlyList<PluginSupportedFeature> GetSupportedFeatures();

  /// <summary>
  /// Gets all supported providers for this plugin
  /// </summary>
  IReadOnlyList<string> GetSupportedProviders();

  /// <summary>
  /// Gets all supported keys for a specific provider
  /// </summary>
  IReadOnlyList<string> GetSupportedKeys(string provider);

  /// <summary>
  /// Populates profile data from external sources
  /// </summary>
  Task<ProfileData> PopulateProfileAsync(ProfilePopulationMetadata metadata, CancellationToken cancellationToken = default);

  /// <summary>
  /// Validates if the plugin can handle the given metadata
  /// </summary>
  bool CanHandle(ProfilePopulationMetadata metadata);
}
