using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.UseCases.Submissions.Retrieve;

/// <summary>
/// Query to retrieve submission data from cache for a specific plugin
/// </summary>
public record RetrieveSubmissionsQuery(
  Guid ProfileId,
  string PluginId,
  string Provider,
  Dictionary<string, object>? AdditionalData = null
 ) : IQuery<Result<ProfileData>>;
