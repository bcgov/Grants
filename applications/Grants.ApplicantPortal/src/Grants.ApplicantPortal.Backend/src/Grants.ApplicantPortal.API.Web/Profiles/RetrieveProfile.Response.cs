namespace Grants.ApplicantPortal.API.Web.Profiles;

public record RetrieveProfileResponse(
    Guid ProfileId,
    string PluginId,
    string Provider,
    string Key,
    string JsonData,
    DateTime PopulatedAt
);
