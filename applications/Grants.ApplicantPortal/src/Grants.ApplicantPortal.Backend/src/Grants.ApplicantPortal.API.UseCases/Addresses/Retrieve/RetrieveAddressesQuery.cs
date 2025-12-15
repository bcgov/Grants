using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.UseCases.Addresses.Retrieve;

/// <summary>
/// Query to retrieve address data from cache for a specific plugin
/// </summary>
public record RetrieveAddressesQuery(
  Guid ProfileId,
  string PluginId,
  string Provider,
  Dictionary<string, object>? AdditionalData = null
 ) : IQuery<Result<ProfileData>>;
