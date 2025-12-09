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
  string? LegalName,
  string? DoingBusinessAs,
  string? Ein,
  int? Founded,
  string? FiscalMonth,
  int? FiscalDay,
  string? Mission,
  string[]? ServiceAreas,
  Guid ProfileId,
  string PluginId,
  string Provider) : ICommand<Result>;
