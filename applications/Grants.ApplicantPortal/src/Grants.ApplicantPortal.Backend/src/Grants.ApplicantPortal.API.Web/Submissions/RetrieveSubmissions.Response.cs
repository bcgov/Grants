namespace Grants.ApplicantPortal.API.Web.Submissions;

public record RetrieveSubmissionsResponse(
    Guid ProfileId,
    string PluginId,
    string Provider,
    string JsonData,
    DateTime PopulatedAt
);
