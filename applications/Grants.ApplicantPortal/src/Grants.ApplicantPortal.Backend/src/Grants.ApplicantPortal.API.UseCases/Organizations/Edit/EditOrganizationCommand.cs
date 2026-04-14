namespace Grants.ApplicantPortal.API.UseCases.Organizations.Edit;

/// <summary>
/// Edit an existing Organization.
/// </summary>
public record EditOrganizationCommand(
  Guid OrganizationId,
  string Name,
  string OrganizationType,
  string OrganizationNumber,
  string Status,
  string? NonRegOrgName,    
  string? FiscalMonth,
  int? FiscalDay,
  uint? OrganizationSize,
  Guid ProfileId,
  string PluginId,
  string Provider,
  string? Subject = null) : ICommand<Result>;
