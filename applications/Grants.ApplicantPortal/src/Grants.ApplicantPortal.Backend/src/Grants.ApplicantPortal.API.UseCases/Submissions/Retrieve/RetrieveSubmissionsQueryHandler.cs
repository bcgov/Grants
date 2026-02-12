using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.UseCases.Submissions.Retrieve;

/// <summary>
/// Query handler for retrieving cached submission data for a specific plugin with automatic hydration
/// and built-in cache stampede protection using .NET 9 HybridCache (L1/L2 caching).
/// 
/// This handler specifically targets submission data by hard-coding the Key to "SUBMISSIONS".
/// </summary>
public class RetrieveSubmissionsQueryHandler(
    IProfileDataRetrievalService profileDataRetrievalService,
    ILogger<RetrieveSubmissionsQueryHandler> logger)
    : IQueryHandler<RetrieveSubmissionsQuery, Result<ProfileData>>
{  
  private const string SubmissionsKey = "SUBMISSIONS";
  
  public async Task<Result<ProfileData>> Handle(RetrieveSubmissionsQuery request, CancellationToken cancellationToken)
  {
    logger.LogInformation("Handling retrieve submissions request for ProfileId: {ProfileId}, PluginId: {PluginId}, Provider: {Provider}",
        request.ProfileId, request.PluginId, request.Provider);

    return await profileDataRetrievalService.RetrieveProfileDataAsync(
      request.ProfileId,
      request.PluginId,
      request.Provider,
      SubmissionsKey,
      request.Subject,
      request.AdditionalData,
      cancellationToken);
  }
}
