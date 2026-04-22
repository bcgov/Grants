namespace Grants.ApplicantPortal.API.Web.Addresses;

public record RetrieveAddressTypesResponse(
    string PluginId,
    IReadOnlyList<AddressTypeDto> Types
);

public record AddressTypeDto(
    string Key,
    string Label
);
