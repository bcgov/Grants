namespace Grants.ApplicantPortal.API.Web.Submissions;

public record RetrieveSubmissionFormResponse(
    Guid ProfileId,
    string PluginId,
    string Provider,
    Guid SubmissionId,
    object Data,
    DateTime PopulatedAt,
    string? CacheStatus = null,
    string? CacheStore = null
);
