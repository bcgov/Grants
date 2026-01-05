namespace Grants.ApplicantPortal.API.Web.Contacts;

public record RetrieveContactsResponse(
    Guid ProfileId,
    string PluginId,
    string Provider,
    object Data,
    DateTime PopulatedAt
);
