namespace Grants.ApplicantPortal.API.Web.Profiles;

public record HydrateProfileResponse(
    Guid ProfileId,
    string PluginId,
    string JsonData,
    DateTime PopulatedAt
);
