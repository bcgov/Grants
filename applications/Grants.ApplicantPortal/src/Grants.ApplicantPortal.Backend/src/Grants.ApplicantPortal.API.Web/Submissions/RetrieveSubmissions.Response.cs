namespace Grants.ApplicantPortal.API.Web.Submissions;

public record RetrieveSubmissionsResponse(
    Guid ProfileId,
    string PluginId,
    string Provider,
    object Data,
    DateTime PopulatedAt,
    string? CacheStatus = null,
    string? CacheStore = null
);
