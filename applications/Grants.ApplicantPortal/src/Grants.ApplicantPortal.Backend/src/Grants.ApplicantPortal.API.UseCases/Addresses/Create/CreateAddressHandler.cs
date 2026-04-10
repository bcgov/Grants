using Grants.ApplicantPortal.API.Core.DTOs;
using Grants.ApplicantPortal.API.Core.Services;

namespace Grants.ApplicantPortal.API.UseCases.Addresses.Create;

public class CreateAddressHandler(
  IAddressManagementService addressManagementService,
  IPluginCacheService pluginCacheService,
  ILogger<CreateAddressHandler> logger)
  : ICommandHandler<CreateAddressCommand, Result<AddressMutationResult>>
{
  public async Task<Result<AddressMutationResult>> Handle(CreateAddressCommand request,
    CancellationToken cancellationToken)
  {
    logger.LogInformation("Creating address for ProfileId: {ProfileId} using Plugin: {PluginId}",
      request.ProfileId, request.PluginId);

    try
    {
      var addressRequest = new CreateAddressRequest(
        request.AddressType,
        request.Street,
        request.City,
        request.Province,
        request.PostalCode,
        request.IsPrimary,
        request.Street2,
        request.Unit,
        request.Country,
        request.ApplicantId);

      var profileContext = new ProfileContext(
        request.ProfileId,
        request.PluginId,
        request.Provider,
        request.Subject);

      var result = await addressManagementService.CreateAddressAsync(
        addressRequest,
        profileContext,
        cancellationToken);

      if (result.IsSuccess)
      {
        logger.LogInformation("Successfully created address {AddressId} for ProfileId: {ProfileId}",
          result.Value, request.ProfileId);

        var primaryId = await PrimaryAddressResolver.GetPrimaryAddressIdAsync(
            pluginCacheService, request.ProfileId, request.PluginId, request.Provider, cancellationToken);

        return new AddressMutationResult(result.Value, primaryId);
      }

      logger.LogWarning("Failed to create address for ProfileId: {ProfileId}. Status: {Status}",
        request.ProfileId, result.Status);

      if (result.Status == ResultStatus.NotFound)
        return Result<AddressMutationResult>.NotFound(result.Errors.ToArray());

      if (result.Status == ResultStatus.Forbidden)
        return Result<AddressMutationResult>.Forbidden();

      return Result<AddressMutationResult>.Invalid(result.ValidationErrors);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Unexpected error creating address for ProfileId: {ProfileId}",
        request.ProfileId);
      return Result<AddressMutationResult>.Error("An unexpected error occurred while creating the address");
    }
  }
}
