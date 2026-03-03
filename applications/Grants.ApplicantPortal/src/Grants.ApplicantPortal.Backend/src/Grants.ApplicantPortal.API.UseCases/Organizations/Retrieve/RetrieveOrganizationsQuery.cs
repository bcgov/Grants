using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.UseCases.Organizations.Retrieve;

/// <summary>
/// Query to retrieve organization data from cache for a specific plugin
/// </summary>
public record RetrieveOrganizationsQuery(
 Guid ProfileId,
 string PluginId,
 string Provider,
 string Subject,
 Dictionary<string, object>? AdditionalData = null
) : IQuery<Result<ProfileData>>;
