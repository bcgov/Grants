namespace Grants.ApplicantPortal.API.Web.Profiles;

public record HydrateProfileResponse(
    Guid ProfileId,
    string PluginId,
    string Provider,
    string Key,
    string JsonData,
    DateTime PopulatedAt
);
