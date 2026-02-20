using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Core.Services;

namespace Grants.ApplicantPortal.API.UseCases.Contacts.SetAsPrimary;

public class SetAsPrimaryContactHandler(
  IContactManagementService contactManagementService,
  IProfileCacheInvalidationService cacheInvalidationService,
  ILogger<SetAsPrimaryContactHandler> logger)
  : ICommandHandler<SetAsPrimaryContactCommand, Result>
{
  public async Task<Result> Handle(SetAsPrimaryContactCommand request, CancellationToken cancellationToken)
  {
    logger.LogInformation("Setting contact {ContactId} as primary for ProfileId: {ProfileId} using Plugin: {PluginId}",
      request.ContactId, request.ProfileId, request.PluginId);

    try
    {
      var profileContext = new ProfileContext(
        request.ProfileId,
        request.PluginId,
        request.Provider);

      var result = await contactManagementService.SetAsPrimaryContactAsync(
        request.ContactId,
        profileContext,
        cancellationToken);

      if (result.IsSuccess)
      {
        logger.LogInformation("Successfully set contact {ContactId} as primary for ProfileId: {ProfileId}",
          request.ContactId, request.ProfileId);
          
        // Invalidate contacts cache so the primary contact change appears immediately
        await cacheInvalidationService.InvalidateProfileDataCacheAsync(
          request.ProfileId,
          request.PluginId,
          request.Provider,
          "CONTACTINFO",
          cancellationToken);
          
        logger.LogDebug("Invalidated contacts cache for ProfileId: {ProfileId} after setting primary contact", 
          request.ProfileId);
      }
      else
      {
        logger.LogWarning("Failed to set contact {ContactId} as primary for ProfileId: {ProfileId}. Status: {Status}",
          request.ContactId, request.ProfileId, result.Status);
      }

      return result;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Unexpected error setting contact {ContactId} as primary for ProfileId: {ProfileId}",
        request.ContactId, request.ProfileId);
      return Result.Error("An unexpected error occurred while setting the contact as primary");
    }
  }
}
