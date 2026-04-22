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
  /// Gets the high-level features supported by this plugin (e.g. ProfilePopulation, ContactManagement).
  /// These are plugin-wide capabilities, not per-provider.
  /// </summary>
  IReadOnlyList<string> GetSupportedFeatures();

  /// <summary>
  /// Asynchronously retrieves the available providers (tenants) for this plugin.
  /// Plugins that fetch providers from an external source should call the upstream API.
  /// Plugins with static providers should return them directly.
  /// An empty list is a valid result when no providers are available for the user.
  /// </summary>
  /// <param name="profileId">The authenticated user's profile identifier</param>
  /// <param name="subject">The authenticated user's subject claim</param>
  /// <param name="cancellationToken">Cancellation token</param>
  Task<IReadOnlyList<ProviderInfo>> GetProvidersAsync(Guid profileId, string subject, CancellationToken cancellationToken = default);

  /// <summary>
  /// Populates profile data from external sources or cache
  /// </summary>
  Task<ProfileData> PopulateProfileAsync(ProfilePopulationMetadata metadata, CancellationToken cancellationToken = default);

  /// <summary>
  /// Validates if the plugin can handle the given metadata
  /// </summary>
  bool CanHandle(ProfilePopulationMetadata metadata);

  /// <summary>
  /// Gets the available contact role options for this plugin.
  /// These are displayed as selectable options when creating or editing contacts.
  /// </summary>
  IReadOnlyList<ContactRoleOption> GetContactRoles() => [];

  /// <summary>
  /// Gets the available address type options for this plugin.
  /// These are displayed as selectable options when creating or editing addresses.
  /// </summary>
  IReadOnlyList<AddressTypeOption> GetAddressTypes() => [];
}
