using Grants.ApplicantPortal.API.UseCases.Payments.Retrieve;
using Grants.ApplicantPortal.API.Web.Auth;
using Grants.ApplicantPortal.API.Web.Extensions;

namespace Grants.ApplicantPortal.API.Web.Payments;

/// <summary>
/// Retrieves payment data for a profile by its unique identifier and plugin ID, with automatic cache hydration.
/// </summary>
/// <param name="mediator"></param>
public class RetrievePayments(IMediator mediator)
  : Endpoint<RetrievePaymentsRequest, RetrievePaymentsResponse>
{
  public override void Configure()
  {
    Get(RetrievePaymentsRequest.Route);
    Policies(AuthPolicies.RequireAuthenticatedUser);
    Summary(s =>
    {
      s.Summary = "Retrieve payment data with automatic hydration";
      s.Description = "Retrieves cached payment data by ProfileId and PluginId. If data is not cached, it will automatically hydrate the cache using the specified plugin and return the results. Cache stampede protection is included to prevent multiple concurrent hydration requests for the same data.";
      s.Responses[200] = "Payment data retrieved successfully (either from cache or after hydration)";
      s.Responses[401] = "Unauthorized - valid JWT token required";
      s.Responses[404] = "Payment data not found or plugin unable to retrieve data";
      s.Responses[400] = "Invalid request or plugin validation failed";
    });
    
    Tags("Payments");
  }

  public override async Task HandleAsync(RetrievePaymentsRequest request, CancellationToken ct)
  {
    // Get the current user's profile ID from the HTTP context
    var profileId = HttpContext.GetRequiredProfileId();
    var subject = HttpContext.User.GetSubject() ?? string.Empty;

    var query = new RetrievePaymentsQuery(profileId, 
      request.PluginId, 
      request.Provider, 
      subject,
      request.Parameters);

    var result = await mediator.Send(query, ct);

    if (result.Status == ResultStatus.NotFound)
    {
      await SendNotFoundAsync(ct);
      return;
    }

    if (result.IsSuccess)
    {
      Response = new RetrievePaymentsResponse(
        result.Value.ProfileId,
        result.Value.PluginId,
        result.Value.Provider,
        result.Value.Data,
        result.Value.PopulatedAt,
        result.Value.CacheStatus,
        result.Value.CacheStore);
      return;
    }

    await SendErrorsAsync(cancellation: ct);
  }
}
