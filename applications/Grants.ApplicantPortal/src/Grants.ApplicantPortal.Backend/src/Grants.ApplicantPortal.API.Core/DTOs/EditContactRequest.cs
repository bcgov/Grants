namespace Grants.ApplicantPortal.API.Core.DTOs;

/// <summary>
/// Request DTO for editing an existing contact
/// </summary>
public record EditContactRequest(
  Guid ContactId,
  string Name,
  string Type,
  bool IsPrimary,
  string? Title = null,
  string? Email = null,
  string? PhoneNumber = null);
