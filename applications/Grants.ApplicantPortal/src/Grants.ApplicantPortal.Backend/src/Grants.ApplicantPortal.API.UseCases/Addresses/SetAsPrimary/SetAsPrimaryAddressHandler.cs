using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Core.Services;

namespace Grants.ApplicantPortal.API.UseCases.Addresses.SetAsPrimary;

public class SetAsPrimaryAddressHandler(
  IAddressManagementService addressManagementService,
  IProfileCacheInvalidationService cacheInvalidationService,
  ILogger<SetAsPrimaryAddressHandler> logger)
  : ICommandHandler<SetAsPrimaryAddressCommand, Result>
{
  public async Task<Result> Handle(SetAsPrimaryAddressCommand request, CancellationToken cancellationToken)
  {
    logger.LogInformation("Setting address {AddressId} as primary for ProfileId: {ProfileId} using Plugin: {PluginId}",
      request.AddressId, request.ProfileId, request.PluginId);

    try
    {
      var profileContext = new ProfileContext(
        request.ProfileId,
        request.PluginId,
        request.Provider);

      var result = await addressManagementService.SetAsPrimaryAddressAsync(
        request.AddressId,
        profileContext,
        cancellationToken);

      if (result.IsSuccess)
      {
        logger.LogInformation("Successfully set address {AddressId} as primary for ProfileId: {ProfileId}",
          request.AddressId, request.ProfileId);
          
        // Invalidate addresses cache so the primary address change appears immediately
        await cacheInvalidationService.InvalidateProfileDataCacheAsync(
          request.ProfileId,
          request.PluginId,
          request.Provider,
          "ADDRESSINFO",
          cancellationToken);
          
        logger.LogDebug("Invalidated addresses cache for ProfileId: {ProfileId} after setting primary address", 
          request.ProfileId);
      }
      else
      {
        logger.LogWarning("Failed to set address {AddressId} as primary for ProfileId: {ProfileId}. Status: {Status}",
          request.AddressId, request.ProfileId, result.Status);
      }

      return result;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Unexpected error setting address {AddressId} as primary for ProfileId: {ProfileId}",
        request.AddressId, request.ProfileId);
      return Result.Error("An unexpected error occurred while setting the address as primary");
    }
  }
}
