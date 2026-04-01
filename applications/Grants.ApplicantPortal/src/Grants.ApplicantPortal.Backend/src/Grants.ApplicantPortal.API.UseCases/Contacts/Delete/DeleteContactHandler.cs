using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Core.Services;

namespace Grants.ApplicantPortal.API.UseCases.Contacts.Delete;

public class DeleteContactHandler(
  IContactManagementService contactManagementService,
  IPluginCacheService pluginCacheService,
  ILogger<DeleteContactHandler> logger)
  : ICommandHandler<DeleteContactCommand, Result<ContactMutationResult>>
{
  public async Task<Result<ContactMutationResult>> Handle(DeleteContactCommand request, CancellationToken cancellationToken)
  {
    logger.LogInformation("Deleting contact {ContactId} for ProfileId: {ProfileId} using Plugin: {PluginId}",
      request.ContactId, request.ProfileId, request.PluginId);

    try
    {
      var profileContext = new ProfileContext(
        request.ProfileId,
        request.PluginId,
        request.Provider,
        request.Subject);

      var result = await contactManagementService.DeleteContactAsync(
        request.ContactId,
        request.ApplicantId,
        profileContext,
        cancellationToken);

      if (result.IsSuccess)
      {
        logger.LogInformation("Successfully deleted contact {ContactId} for ProfileId: {ProfileId}",
          request.ContactId, request.ProfileId);

        var primaryId = await PrimaryContactResolver.GetPrimaryContactIdAsync(
            pluginCacheService, request.ProfileId, request.PluginId, request.Provider, cancellationToken);

        return new ContactMutationResult(request.ContactId, primaryId);
      }

      logger.LogWarning("Failed to delete contact {ContactId} for ProfileId: {ProfileId}. Status: {Status}",
        request.ContactId, request.ProfileId, result.Status);

      if (result.Status == ResultStatus.NotFound)
        return Result<ContactMutationResult>.NotFound();

      return Result<ContactMutationResult>.Invalid(result.ValidationErrors);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Unexpected error deleting contact {ContactId} for ProfileId: {ProfileId}",
        request.ContactId, request.ProfileId);
      return Result<ContactMutationResult>.Error("An unexpected error occurred while deleting the contact");
    }
  }
}
