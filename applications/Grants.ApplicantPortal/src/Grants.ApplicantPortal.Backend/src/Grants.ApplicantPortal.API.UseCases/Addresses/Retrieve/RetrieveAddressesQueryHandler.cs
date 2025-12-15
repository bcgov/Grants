using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.UseCases.Addresses.Retrieve;

/// <summary>
/// Query handler for retrieving cached address data for a specific plugin with automatic hydration
/// and built-in cache stampede protection using .NET 9 HybridCache (L1/L2 caching).
/// 
/// This handler specifically targets address data by hard-coding the Key to "ADDRESSES".
/// </summary>
public class RetrieveAddressesQueryHandler(
    IProfileDataRetrievalService profileDataRetrievalService,
    ILogger<RetrieveAddressesQueryHandler> logger)
    : IQueryHandler<RetrieveAddressesQuery, Result<ProfileData>>
{  
  private const string AddressesKey = "ADDRESSES";
  
  public async Task<Result<ProfileData>> Handle(RetrieveAddressesQuery request, CancellationToken cancellationToken)
  {
    logger.LogInformation("Handling retrieve addresses request for ProfileId: {ProfileId}, PluginId: {PluginId}, Provider: {Provider}",
        request.ProfileId, request.PluginId, request.Provider);

    return await profileDataRetrievalService.RetrieveProfileDataAsync(
      request.ProfileId,
      request.PluginId,
      request.Provider,
      AddressesKey,
      request.AdditionalData,
      cancellationToken);
  }
}
