using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.UseCases.Payments.Retrieve;

/// <summary>
/// Query handler for retrieving cached payment data for a specific plugin with automatic hydration
/// and built-in cache stampede protection using .NET 9 HybridCache (L1/L2 caching).
/// 
/// This handler specifically targets payment data by hard-coding the Key to "PAYMENTINFO".
/// </summary>
public class RetrievePaymentsQueryHandler(
    IProfileDataRetrievalService profileDataRetrievalService,
    ILogger<RetrievePaymentsQueryHandler> logger)
    : IQueryHandler<RetrievePaymentsQuery, Result<ProfileData>>
{  
  private const string PaymentsKey = "PAYMENTINFO";
  
  public async Task<Result<ProfileData>> Handle(RetrievePaymentsQuery request, CancellationToken cancellationToken)
  {
    logger.LogInformation("Handling retrieve payments request for ProfileId: {ProfileId}, PluginId: {PluginId}, Provider: {Provider}",
        request.ProfileId, request.PluginId, request.Provider);

    return await profileDataRetrievalService.RetrieveProfileDataAsync(
      request.ProfileId,
      request.PluginId,
      request.Provider,
      PaymentsKey,
      request.Subject,
      request.AdditionalData,
      cancellationToken);
  }
}
