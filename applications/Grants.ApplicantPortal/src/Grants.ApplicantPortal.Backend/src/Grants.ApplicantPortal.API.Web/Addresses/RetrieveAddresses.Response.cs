namespace Grants.ApplicantPortal.API.Web.Addresses;

public record RetrieveAddressesResponse(
    Guid ProfileId,
    string PluginId,
    string Provider,
    object Data,
    DateTime PopulatedAt
);
