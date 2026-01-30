using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Core.Services;

namespace Grants.ApplicantPortal.API.UseCases.Contacts.Edit;

public class EditContactHandler(
  IContactManagementService contactManagementService,
  IProfileCacheInvalidationService cacheInvalidationService,
  ILogger<EditContactHandler> logger)
  : ICommandHandler<EditContactCommand, Result>
{
  public async Task<Result> Handle(EditContactCommand request, CancellationToken cancellationToken)
  {
    logger.LogInformation("Editing contact {ContactId} for ProfileId: {ProfileId} using Plugin: {PluginId}",
      request.ContactId, request.ProfileId, request.PluginId);

    try
    {
      var editRequest = new EditContactRequest(
        request.ContactId,
        request.Name,
        request.Type,
        request.IsPrimary,
        request.Title,
        request.Email,
        request.PhoneNumber);

      var profileContext = new ProfileContext(
        request.ProfileId,
        request.PluginId,
        request.Provider);

      var result = await contactManagementService.EditContactAsync(
        editRequest,
        profileContext,
        cancellationToken);

      if (result.IsSuccess)
      {
        logger.LogInformation("Successfully edited contact {ContactId} for ProfileId: {ProfileId}",
          request.ContactId, request.ProfileId);
          
        // Invalidate contacts cache so the updated contact appears immediately
        await cacheInvalidationService.InvalidateProfileDataCacheAsync(
          request.ProfileId,
          request.PluginId,
          request.Provider,
          "CONTACTS",
          cancellationToken);
          
        logger.LogDebug("Invalidated contacts cache for ProfileId: {ProfileId} after contact edit", 
          request.ProfileId);
      }
      else
      {
        logger.LogWarning("Failed to edit contact {ContactId} for ProfileId: {ProfileId}. Status: {Status}",
          request.ContactId, request.ProfileId, result.Status);
      }

      return result;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Unexpected error editing contact {ContactId} for ProfileId: {ProfileId}",
        request.ContactId, request.ProfileId);
      return Result.Error("An unexpected error occurred while editing the contact");
    }
  }
}
