namespace Grants.ApplicantPortal.API.Web.Organizations;

public record RetrieveOrganizationsResponse(
    Guid ProfileId,
    string PluginId,
    string Provider,
    object Data,
    DateTime PopulatedAt,
    string? CacheStatus = null,
    string? CacheStore = null
);
