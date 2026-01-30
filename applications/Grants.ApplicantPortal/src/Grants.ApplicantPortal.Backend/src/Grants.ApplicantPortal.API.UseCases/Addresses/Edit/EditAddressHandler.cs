using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Core.Services;

namespace Grants.ApplicantPortal.API.UseCases.Addresses.Edit;

public class EditAddressHandler(
  IAddressManagementService addressManagementService,
  IProfileCacheInvalidationService cacheInvalidationService,
  ILogger<EditAddressHandler> logger)
  : ICommandHandler<EditAddressCommand, Result>
{
  public async Task<Result> Handle(EditAddressCommand request, CancellationToken cancellationToken)
  {
    logger.LogInformation("Editing address {AddressId} for ProfileId: {ProfileId} using Plugin: {PluginId}",
      request.AddressId, request.ProfileId, request.PluginId);

    try
    {
      var editRequest = new EditAddressRequest(
        request.AddressId,
        request.Type,
        request.Address,
        request.City,
        request.Province,
        request.PostalCode,
        request.IsPrimary,
        request.Country);

      var profileContext = new ProfileContext(
        request.ProfileId,
        request.PluginId,
        request.Provider);

      var result = await addressManagementService.EditAddressAsync(
        editRequest,
        profileContext,
        cancellationToken);

      if (result.IsSuccess)
      {
        logger.LogInformation("Successfully edited address {AddressId} for ProfileId: {ProfileId}",
          request.AddressId, request.ProfileId);
          
        // Invalidate addresses cache so the updated address appears immediately
        await cacheInvalidationService.InvalidateProfileDataCacheAsync(
          request.ProfileId,
          request.PluginId,
          request.Provider,
          "ADDRESSES",
          cancellationToken);
          
        logger.LogDebug("Invalidated addresses cache for ProfileId: {ProfileId} after address edit", 
          request.ProfileId);
      }
      else
      {
        logger.LogWarning("Failed to edit address {AddressId} for ProfileId: {ProfileId}. Status: {Status}",
          request.AddressId, request.ProfileId, result.Status);
      }

      return result;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Unexpected error editing address {AddressId} for ProfileId: {ProfileId}",
        request.AddressId, request.ProfileId);
      return Result.Error("An unexpected error occurred while editing the address");
    }
  }
}
