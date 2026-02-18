namespace Grants.ApplicantPortal.API.Core.DTOs;

/// <summary>
/// Request DTO for creating a new contact
/// </summary>
public record CreateContactRequest(
  string Name,
  string ContactType,
  bool IsPrimary,
  string? Title = null,
  string? Email = null,
  string? HomePhoneNumber = null,
  string? MobilePhoneNumber = null,
  string? WorkPhoneNumber = null,
  string? WorkPhoneExtension = null,
  string? Role = null);
