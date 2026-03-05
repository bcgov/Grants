using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Core.Services;

namespace Grants.ApplicantPortal.API.UseCases.Organizations.Edit;

public class EditOrganizationHandler(
  IOrganizationManagementService organizationManagementService,
  ILogger<EditOrganizationHandler> logger)
  : ICommandHandler<EditOrganizationCommand, Result>
{
  public async Task<Result> Handle(EditOrganizationCommand request, CancellationToken cancellationToken)
  {
    logger.LogInformation("Editing organization {OrganizationId} for ProfileId: {ProfileId} using Plugin: {PluginId}",
      request.OrganizationId, request.ProfileId, request.PluginId);

    try
    {
      var editRequest = new EditOrganizationRequest(
        request.OrganizationId,
        request.Name,
        request.OrganizationType,
        request.OrganizationNumber,
        request.Status,
        LegalName: null, // We don't have LegalName in the command, keeping it null for now
        NonRegOrgName: request.NonRegOrgName,
        request.FiscalMonth,
        request.FiscalDay,
        request.OrganizationSize);

      var profileContext = new ProfileContext(
        request.ProfileId,
        request.PluginId,
        request.Provider);

      var result = await organizationManagementService.EditOrganizationAsync(
        editRequest,
        profileContext,
        cancellationToken);

      if (result.IsSuccess)
      {
        logger.LogInformation("Successfully edited organization {OrganizationId} for ProfileId: {ProfileId}",
          request.OrganizationId, request.ProfileId);
      }
      else
      {
        logger.LogWarning("Failed to edit organization {OrganizationId} for ProfileId: {ProfileId}. Status: {Status}",
          request.OrganizationId, request.ProfileId, result.Status);
      }

      return result;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Unexpected error editing organization {OrganizationId} for ProfileId: {ProfileId}",
        request.OrganizationId, request.ProfileId);
      return Result.Error("An unexpected error occurred while editing the organization");
    }
  }
}
