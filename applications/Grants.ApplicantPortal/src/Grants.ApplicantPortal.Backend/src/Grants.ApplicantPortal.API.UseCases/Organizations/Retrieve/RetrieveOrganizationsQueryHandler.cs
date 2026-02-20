using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.UseCases.Organizations.Retrieve;

/// <summary>
/// Query handler for retrieving cached organization data for a specific plugin with automatic hydration
/// and built-in cache stampede protection using .NET 9 HybridCache (L1/L2 caching).
/// 
/// This handler specifically targets organization data by hard-coding the Key to "ORGINFO".
/// </summary>
public class RetrieveOrganizationsQueryHandler(
    IProfileDataRetrievalService profileDataRetrievalService,
    ILogger<RetrieveOrganizationsQueryHandler> logger)
    : IQueryHandler<RetrieveOrganizationsQuery, Result<ProfileData>>
{  
  private const string OrganizationKey = "ORGINFO";
  
  public async Task<Result<ProfileData>> Handle(RetrieveOrganizationsQuery request, CancellationToken cancellationToken)
  {
    logger.LogInformation("Handling retrieve organizations request for ProfileId: {ProfileId}, PluginId: {PluginId}, Provider: {Provider}",
        request.ProfileId, request.PluginId, request.Provider);

    return await profileDataRetrievalService.RetrieveProfileDataAsync(
      request.ProfileId,
      request.PluginId,
      request.Provider,
      OrganizationKey,
      request.Subject,
      request.AdditionalData,
      cancellationToken);
  }
}
