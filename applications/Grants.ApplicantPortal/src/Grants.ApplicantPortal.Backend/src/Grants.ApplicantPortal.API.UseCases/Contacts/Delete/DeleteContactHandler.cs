using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Core.Services;

namespace Grants.ApplicantPortal.API.UseCases.Contacts.Delete;

public class DeleteContactHandler(
  IContactManagementService contactManagementService,
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
        request.Provider,
        request.Subject);

      var result = await contactManagementService.DeleteContactAsync(
        request.ContactId,
        profileContext,
        cancellationToken);

      if (result.IsSuccess)
      {
        logger.LogInformation("Successfully deleted contact {ContactId} for ProfileId: {ProfileId}",
          request.ContactId, request.ProfileId);
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
