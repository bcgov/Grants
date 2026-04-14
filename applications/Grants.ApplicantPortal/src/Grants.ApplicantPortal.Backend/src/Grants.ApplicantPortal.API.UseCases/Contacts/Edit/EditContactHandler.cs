using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Core.Services;

namespace Grants.ApplicantPortal.API.UseCases.Contacts.Edit;

public class EditContactHandler(
  IContactManagementService contactManagementService,
  IPluginCacheService pluginCacheService,
  ILogger<EditContactHandler> logger)
  : ICommandHandler<EditContactCommand, Result<ContactMutationResult>>
{
  public async Task<Result<ContactMutationResult>> Handle(EditContactCommand request, CancellationToken cancellationToken)
  {
    logger.LogInformation("Editing contact {ContactId} for ProfileId: {ProfileId} using Plugin: {PluginId}",
      request.ContactId, request.ProfileId, request.PluginId);

    try
    {
      var editRequest = new EditContactRequest(
        request.ContactId,
        request.Name,
        request.ContactType,
        request.IsPrimary,
        request.Title,
        request.Email,
        request.HomePhoneNumber,
        request.MobilePhoneNumber,
        request.WorkPhoneNumber,
        request.WorkPhoneExtension,
        request.Role,
        request.ApplicantId);

      var profileContext = new ProfileContext(
        request.ProfileId,
        request.PluginId,
        request.Provider,
        request.Subject);

      var result = await contactManagementService.EditContactAsync(
        editRequest,
        profileContext,
        cancellationToken);

      if (result.IsSuccess)
      {
        logger.LogInformation("Successfully edited contact {ContactId} for ProfileId: {ProfileId}",
          request.ContactId, request.ProfileId);

        var primaryId = await PrimaryContactResolver.GetPrimaryContactIdAsync(
            pluginCacheService, request.ProfileId, request.PluginId, request.Provider, cancellationToken);

        return new ContactMutationResult(request.ContactId, primaryId);
      }

      logger.LogWarning("Failed to edit contact {ContactId} for ProfileId: {ProfileId}. Status: {Status}",
        request.ContactId, request.ProfileId, result.Status);

      if (result.Status == ResultStatus.NotFound)
        return Result<ContactMutationResult>.NotFound();

      if (result.Status == ResultStatus.Forbidden)
        return Result<ContactMutationResult>.Forbidden();

      return Result<ContactMutationResult>.Invalid(result.ValidationErrors);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Unexpected error editing contact {ContactId} for ProfileId: {ProfileId}",
        request.ContactId, request.ProfileId);
      return Result<ContactMutationResult>.Error("An unexpected error occurred while editing the contact");
    }
  }
}
