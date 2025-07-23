namespace Grants.ApplicantPortal.API.Web.Profiles;

public record RetrieveProfileResponse(
    Guid ProfileId,
    string PluginId,
    string JsonData,
    DateTime PopulatedAt    
);
