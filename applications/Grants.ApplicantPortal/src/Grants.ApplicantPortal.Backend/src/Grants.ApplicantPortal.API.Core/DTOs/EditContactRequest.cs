namespace Grants.ApplicantPortal.API.Core.DTOs;

/// <summary>
/// Request DTO for editing an existing contact
/// </summary>
public record EditContactRequest(
  Guid ContactId,
  string Name,
  string ContactType,
  bool IsPrimary,
  string? Title = null,
  string? Email = null,
  string? HomePhoneNumber = null,
  string? MobilePhoneNumber = null,
  string? WorkPhoneNumber = null,
  string? WorkPhoneExtension = null,
  string? Role = null,
  Guid ApplicantId = default);
