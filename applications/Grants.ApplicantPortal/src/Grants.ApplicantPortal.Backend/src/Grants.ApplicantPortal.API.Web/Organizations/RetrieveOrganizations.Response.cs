namespace Grants.ApplicantPortal.API.Web.Organizations;

public record RetrieveOrganizationsResponse(
    Guid ProfileId,
    string PluginId,
    string Provider,
    string JsonData,
    DateTime PopulatedAt
);
