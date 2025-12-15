namespace Grants.ApplicantPortal.API.Web.Contacts;

public record RetrieveContactsResponse(
    Guid ProfileId,
    string PluginId,
    string Provider,
    string JsonData,
    DateTime PopulatedAt
);
