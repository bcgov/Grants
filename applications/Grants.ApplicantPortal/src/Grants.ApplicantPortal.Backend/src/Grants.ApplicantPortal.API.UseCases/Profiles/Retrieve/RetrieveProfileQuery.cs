using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.UseCases.Profiles.Retrieve;

/// <summary>
/// Query to retrieve profile data from cache for a specific plugin
/// </summary>
public record RetrieveProfileQuery(
  Guid ProfileId,
  string PluginId,
  string Provider,
  string Key,
  Dictionary<string, object>? AdditionalData = null
 ) : IQuery<Result<ProfileData>>;
