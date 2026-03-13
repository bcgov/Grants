using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Core.Services;

namespace Grants.ApplicantPortal.API.UseCases.Addresses.Edit;

public class EditAddressHandler(
  IAddressManagementService addressManagementService,
  IPluginCacheService pluginCacheService,
  ILogger<EditAddressHandler> logger)
  : ICommandHandler<EditAddressCommand, Result<AddressMutationResult>>
{
  public async Task<Result<AddressMutationResult>> Handle(EditAddressCommand request, CancellationToken cancellationToken)
  {
    logger.LogInformation("Editing address {AddressId} for ProfileId: {ProfileId} using Plugin: {PluginId}",
      request.AddressId, request.ProfileId, request.PluginId);

    try
    {
      var editRequest = new EditAddressRequest(
        request.AddressId,
        request.AddressType,
        request.Street,
        request.City,
        request.Province,
        request.PostalCode,
        request.IsPrimary,
        request.Street2,
        request.Unit,
        request.Country);

      var profileContext = new ProfileContext(
        request.ProfileId,
        request.PluginId,
        request.Provider,
        request.Subject);

      var result = await addressManagementService.EditAddressAsync(
        editRequest,
        profileContext,
        cancellationToken);

      if (result.IsSuccess)
      {
        logger.LogInformation("Successfully edited address {AddressId} for ProfileId: {ProfileId}",
          request.AddressId, request.ProfileId);

        var primaryId = await PrimaryAddressResolver.GetPrimaryAddressIdAsync(
            pluginCacheService, request.ProfileId, request.PluginId, request.Provider, cancellationToken);

        return new AddressMutationResult(request.AddressId, primaryId);
      }

      logger.LogWarning("Failed to edit address {AddressId} for ProfileId: {ProfileId}. Status: {Status}",
        request.AddressId, request.ProfileId, result.Status);

      if (result.Status == ResultStatus.NotFound)
        return Result<AddressMutationResult>.NotFound();

      return Result<AddressMutationResult>.Invalid(result.ValidationErrors);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Unexpected error editing address {AddressId} for ProfileId: {ProfileId}",
        request.AddressId, request.ProfileId);
      return Result<AddressMutationResult>.Error("An unexpected error occurred while editing the address");
    }
  }
}
