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
  string? NonRegOrgName = null,
  string? FiscalMonth = null,
  int? FiscalDay = null,
  uint? OrganizationSize = null);
