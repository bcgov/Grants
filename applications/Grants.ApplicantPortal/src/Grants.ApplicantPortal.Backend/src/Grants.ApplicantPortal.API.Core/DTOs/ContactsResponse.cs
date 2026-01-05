namespace Grants.ApplicantPortal.API.Core.DTOs;

/// <summary>
/// Typed response for contacts data
/// </summary>
public record ContactsResponse(
    IReadOnlyList<ContactResponse> Contacts,
    ContactsSummary Summary
);

/// <summary>
/// Individual contact information
/// </summary>
public record ContactResponse(
    string Id,
    string Type,
    string Name,
    string Email,
    string Phone,
    string Title,
    bool IsPrimary,
    bool IsActive,
    DateTime LastUpdated,
    bool AllowEdit
);

/// <summary>
/// Summary information for contacts
/// </summary>
public record ContactsSummary(
    int TotalContacts,
    int PrimaryContactCount,
    int ActiveContactCount
);
