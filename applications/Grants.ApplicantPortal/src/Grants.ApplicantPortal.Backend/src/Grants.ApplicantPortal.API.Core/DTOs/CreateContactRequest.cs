namespace Grants.ApplicantPortal.API.Core.DTOs;

/// <summary>
/// Request DTO for creating a new contact
/// </summary>
public record CreateContactRequest(
  string Name,
  string Type,
  bool IsPrimary,
  string? Title = null,
  string? Email = null,
  string? PhoneNumber = null);
