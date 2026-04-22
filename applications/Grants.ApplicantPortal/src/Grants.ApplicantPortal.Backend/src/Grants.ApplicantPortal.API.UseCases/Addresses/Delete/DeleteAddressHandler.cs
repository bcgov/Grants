using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Core.Services;

namespace Grants.ApplicantPortal.API.UseCases.Addresses.Delete;

public class DeleteAddressHandler(
  IAddressManagementService addressManagementService,
  IPluginCacheService pluginCacheService,
  ILogger<DeleteAddressHandler> logger)
  : ICommandHandler<DeleteAddressCommand, Result<AddressMutationResult>>
{
  public async Task<Result<AddressMutationResult>> Handle(DeleteAddressCommand request, CancellationToken cancellationToken)
  {
    logger.LogInformation("Deleting address {AddressId} for ProfileId: {ProfileId} using Plugin: {PluginId}",
      request.AddressId, request.ProfileId, request.PluginId);

    try
    {
      var profileContext = new ProfileContext(
        request.ProfileId,
        request.PluginId,
        request.Provider,
        request.Subject);

      var result = await addressManagementService.DeleteAddressAsync(
        request.AddressId,
        request.ApplicantId,
        profileContext,
        cancellationToken);

      if (result.IsSuccess)
      {
        logger.LogInformation("Successfully deleted address {AddressId} for ProfileId: {ProfileId}",
          request.AddressId, request.ProfileId);

        var primaryId = await PrimaryAddressResolver.GetPrimaryAddressIdAsync(
            pluginCacheService, request.ProfileId, request.PluginId, request.Provider, cancellationToken);

        return new AddressMutationResult(request.AddressId, primaryId);
      }

      logger.LogWarning("Failed to delete address {AddressId} for ProfileId: {ProfileId}. Status: {Status}",
        request.AddressId, request.ProfileId, result.Status);

      if (result.Status == ResultStatus.NotFound)
        return Result<AddressMutationResult>.NotFound();

      if (result.Status == ResultStatus.Forbidden)
        return Result<AddressMutationResult>.Forbidden();

      return Result<AddressMutationResult>.Invalid(result.ValidationErrors);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Unexpected error deleting address {AddressId} for ProfileId: {ProfileId}",
        request.AddressId, request.ProfileId);
      return Result<AddressMutationResult>.Error("An unexpected error occurred while deleting the address");
    }
  }
}
