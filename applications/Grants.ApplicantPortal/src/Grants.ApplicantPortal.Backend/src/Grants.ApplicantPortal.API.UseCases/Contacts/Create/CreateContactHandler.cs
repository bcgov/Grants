using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Core.Services;

namespace Grants.ApplicantPortal.API.UseCases.Contacts.Create;

public class CreateContactHandler(
  IContactManagementService contactManagementService,
  IPluginCacheService pluginCacheService,
  ILogger<CreateContactHandler> logger)
  : ICommandHandler<CreateContactCommand, Result<ContactMutationResult>>
{
  public async Task<Result<ContactMutationResult>> Handle(CreateContactCommand request,
    CancellationToken cancellationToken)
  {
    logger.LogInformation("Creating contact: {Name} for ProfileId: {ProfileId} using Plugin: {PluginId}",
      request.Name, request.ProfileId, request.PluginId);

    try
    {
      var contactRequest = new CreateContactRequest(
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

      var result = await contactManagementService.CreateContactAsync(
        contactRequest,
        profileContext,
        cancellationToken);

      if (result.IsSuccess)
      {
        logger.LogInformation("Successfully created contact {ContactId} for ProfileId: {ProfileId}",
          result.Value, request.ProfileId);

        var primaryId = await PrimaryContactResolver.GetPrimaryContactIdAsync(
            pluginCacheService, request.ProfileId, request.PluginId, request.Provider, cancellationToken);

        return new ContactMutationResult(result.Value, primaryId);
      }

      logger.LogWarning("Failed to create contact for ProfileId: {ProfileId}. Status: {Status}",
        request.ProfileId, result.Status);

      if (result.Status == ResultStatus.NotFound)
        return Result<ContactMutationResult>.NotFound(result.Errors.ToArray());

      if (result.Status == ResultStatus.Forbidden)
        return Result<ContactMutationResult>.Forbidden();

      return Result<ContactMutationResult>.Invalid(result.ValidationErrors);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Unexpected error creating contact for ProfileId: {ProfileId}",
        request.ProfileId);
      return Result<ContactMutationResult>.Error("An unexpected error occurred while creating the contact");
    }
  }
}
