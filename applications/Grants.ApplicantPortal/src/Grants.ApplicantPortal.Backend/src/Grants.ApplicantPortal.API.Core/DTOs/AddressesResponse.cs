namespace Grants.ApplicantPortal.API.Core.DTOs;

/// <summary>
/// Typed response for addresses data
/// </summary>
public record AddressesResponse(
    IReadOnlyList<AddressResponse> Addresses,
    AddressesSummary Summary
);

/// <summary>
/// Individual address information
/// </summary>
public record AddressResponse(
    string Id,
    string AddressId,
    string Type,
    string AddressLine1,
    string? AddressLine2,
    string Street,
    string City,
    string Province,
    string PostalCode,
    string Country,
    bool IsPrimary,
    bool IsActive,
    DateTime LastVerified,
    bool AllowEdit
);

/// <summary>
/// Summary information for addresses
/// </summary>
public record AddressesSummary(
    int TotalAddresses,
    int PrimaryAddressCount,
    int ActiveAddressCount
);
