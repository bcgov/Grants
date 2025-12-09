namespace Grants.ApplicantPortal.API.Core.DTOs;

/// <summary>
/// Request DTO for editing an existing organization
/// </summary>
public record EditOrganizationRequest(
  Guid OrganizationId,
  string Name,
  string OrganizationType,
  string OrganizationNumber,
  string Status,
  string? LegalName = null,
  string? DoingBusinessAs = null,
  string? Ein = null,
  int? Founded = null,
  string? FiscalMonth = null,
  int? FiscalDay = null,
  string? Mission = null,
  string[]? ServiceAreas = null);
