using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Core.Services;

namespace Grants.ApplicantPortal.API.UseCases.Contacts.Create;

public class CreateContactHandler(
  IContactManagementService contactManagementService,
  IProfileCacheInvalidationService cacheInvalidationService,
  ILogger<CreateContactHandler> logger)
  : ICommandHandler<CreateContactCommand, Result<Guid>>
{
  public async Task<Result<Guid>> Handle(CreateContactCommand request,
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
        request.Role);

      var profileContext = new ProfileContext(
        request.ProfileId,
        request.PluginId,
        request.Provider);

      var result = await contactManagementService.CreateContactAsync(
        contactRequest,
        profileContext,
        cancellationToken);

      if (result.IsSuccess)
      {
        logger.LogInformation("Successfully created contact {ContactId} for ProfileId: {ProfileId}",
          result.Value, request.ProfileId);
          
        // Invalidate contacts cache so the new contact appears immediately
        await cacheInvalidationService.InvalidateProfileDataCacheAsync(
          request.ProfileId,
          request.PluginId,
          request.Provider,
          "CONTACTINFO",
          cancellationToken);
          
        logger.LogDebug("Invalidated contacts cache for ProfileId: {ProfileId} after contact creation", 
          request.ProfileId);
      }
      else
      {
        logger.LogWarning("Failed to create contact for ProfileId: {ProfileId}. Status: {Status}",
          request.ProfileId, result.Status);
      }

      return result;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Unexpected error creating contact for ProfileId: {ProfileId}",
        request.ProfileId);
      return Result<Guid>.Error("An unexpected error occurred while creating the contact");
    }
  }
}
