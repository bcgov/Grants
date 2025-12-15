namespace Grants.ApplicantPortal.API.Web.Addresses;

public record RetrieveAddressesResponse(
    Guid ProfileId,
    string PluginId,
    string Provider,
    string JsonData,
    DateTime PopulatedAt
);
