using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Core.Services;

namespace Grants.ApplicantPortal.API.UseCases.Addresses.SetAsPrimary;

public class SetAsPrimaryAddressHandler(
  IAddressManagementService addressManagementService,
  IPluginCacheService pluginCacheService,
  ILogger<SetAsPrimaryAddressHandler> logger)
  : ICommandHandler<SetAsPrimaryAddressCommand, Result<AddressMutationResult>>
{
  public async Task<Result<AddressMutationResult>> Handle(SetAsPrimaryAddressCommand request, CancellationToken cancellationToken)
  {
    logger.LogInformation("Setting address {AddressId} as primary for ProfileId: {ProfileId} using Plugin: {PluginId}",
      request.AddressId, request.ProfileId, request.PluginId);

    try
    {
      var profileContext = new ProfileContext(
        request.ProfileId,
        request.PluginId,
        request.Provider,
        request.Subject);

      var result = await addressManagementService.SetAsPrimaryAddressAsync(
        request.AddressId,
        profileContext,
        cancellationToken);

      if (result.IsSuccess)
      {
        logger.LogInformation("Successfully set address {AddressId} as primary for ProfileId: {ProfileId}",
          request.AddressId, request.ProfileId);

        var primaryId = await PrimaryAddressResolver.GetPrimaryAddressIdAsync(
            pluginCacheService, request.ProfileId, request.PluginId, request.Provider, cancellationToken);

        return new AddressMutationResult(request.AddressId, primaryId);
      }

      logger.LogWarning("Failed to set address {AddressId} as primary for ProfileId: {ProfileId}. Status: {Status}",
        request.AddressId, request.ProfileId, result.Status);

      if (result.Status == ResultStatus.NotFound)
        return Result<AddressMutationResult>.NotFound();

      return Result<AddressMutationResult>.Invalid(result.ValidationErrors);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Unexpected error setting address {AddressId} as primary for ProfileId: {ProfileId}",
        request.AddressId, request.ProfileId);
      return Result<AddressMutationResult>.Error("An unexpected error occurred while setting the address as primary");
    }
  }
}
