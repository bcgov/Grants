namespace Grants.ApplicantPortal.API.Core.DTOs;

/// <summary>
/// Typed response for organization data
/// </summary>
public record OrganizationResponse(
    OrganizationInfo OrganizationInfo
);

/// <summary>
/// Organization information
/// </summary>
public record OrganizationInfo(
    string OrgName,
    string OrgNumber,
    string OrgStatus,
    string OrganizationType,
    string? NonRegOrgName,
    string OrgSize,
    string FiscalMonth,
    int FiscalDay,
    string OrganizationId,
    string LegalName,
    string DoingBusinessAs,
    string EIN,
    int Founded,
    OrganizationAddress Address,
    OrganizationContactInfo ContactInfo,
    string Mission,
    IReadOnlyList<string> ServicesAreas,
    IReadOnlyList<Certification> Certifications,
    object Program1Specific, // This could be further typed based on specific program needs
    DateTime LastUpdated,
    bool AllowEdit
);

/// <summary>
/// Organization address
/// </summary>
public record OrganizationAddress(
    string Street,
    string City,
    string State,
    string ZipCode,
    string Country
);

/// <summary>
/// Organization contact information
/// </summary>
public record OrganizationContactInfo(
    Contact PrimaryContact,
    Contact GrantsContact
);

/// <summary>
/// Contact information
/// </summary>
public record Contact(
    string Name,
    string Title,
    string Email,
    string Phone
);

/// <summary>
/// Certification information
/// </summary>
public record Certification(
    string Type,
    DateTime ValidUntil
);
