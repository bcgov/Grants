using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Core.Services;

namespace Grants.ApplicantPortal.API.UseCases.Contacts.Delete;

public class DeleteContactHandler(
  IContactManagementService contactManagementService,
  IProfileCacheInvalidationService cacheInvalidationService,
  ILogger<DeleteContactHandler> logger)
  : ICommandHandler<DeleteContactCommand, Result>
{
  public async Task<Result> Handle(DeleteContactCommand request, CancellationToken cancellationToken)
  {
    logger.LogInformation("Deleting contact {ContactId} for ProfileId: {ProfileId} using Plugin: {PluginId}",
      request.ContactId, request.ProfileId, request.PluginId);

    try
    {
      var profileContext = new ProfileContext(
        request.ProfileId,
        request.PluginId,
        request.Provider);

      var result = await contactManagementService.DeleteContactAsync(
        request.ContactId,
        profileContext,
        cancellationToken);

      if (result.IsSuccess)
      {
        logger.LogInformation("Successfully deleted contact {ContactId} for ProfileId: {ProfileId}",
          request.ContactId, request.ProfileId);
          
        // Invalidate contacts cache so the deleted contact disappears immediately
        await cacheInvalidationService.InvalidateProfileDataCacheAsync(
          request.ProfileId,
          request.PluginId,
          request.Provider,
          "CONTACTS");
          
        logger.LogDebug("Invalidated contacts cache for ProfileId: {ProfileId} after contact deletion", 
          request.ProfileId);
      }
      else
      {
        logger.LogWarning("Failed to delete contact {ContactId} for ProfileId: {ProfileId}. Status: {Status}",
          request.ContactId, request.ProfileId, result.Status);
      }

      return result;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Unexpected error deleting contact {ContactId} for ProfileId: {ProfileId}",
        request.ContactId, request.ProfileId);
      return Result.Error("An unexpected error occurred while deleting the contact");
    }
  }
}
