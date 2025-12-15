using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.UseCases.Contacts.Retrieve;

/// <summary>
/// Query handler for retrieving cached contact data for a specific plugin with automatic hydration
/// and built-in cache stampede protection using .NET 9 HybridCache (L1/L2 caching).
/// 
/// This handler specifically targets contact data by hard-coding the Key to "CONTACTS".
/// </summary>
public class RetrieveContactsQueryHandler(
    IProfileDataRetrievalService profileDataRetrievalService,
    ILogger<RetrieveContactsQueryHandler> logger)
    : IQueryHandler<RetrieveContactsQuery, Result<ProfileData>>
{  
  private const string ContactsKey = "CONTACTS";
  
  public async Task<Result<ProfileData>> Handle(RetrieveContactsQuery request, CancellationToken cancellationToken)
  {
    logger.LogInformation("Handling retrieve contacts request for ProfileId: {ProfileId}, PluginId: {PluginId}, Provider: {Provider}",
        request.ProfileId, request.PluginId, request.Provider);

    return await profileDataRetrievalService.RetrieveProfileDataAsync(
      request.ProfileId,
      request.PluginId,
      request.Provider,
      ContactsKey,
      request.AdditionalData,
      cancellationToken);
  }
}
