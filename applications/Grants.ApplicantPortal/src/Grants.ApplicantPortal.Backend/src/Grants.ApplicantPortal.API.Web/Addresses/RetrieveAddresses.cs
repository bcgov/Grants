using Grants.ApplicantPortal.API.UseCases.Addresses.Retrieve;
using Grants.ApplicantPortal.API.Web.Auth;

namespace Grants.ApplicantPortal.API.Web.Addresses;

/// <summary>
/// Retrieves address data for a profile by its unique identifier and plugin ID, with automatic cache hydration.
/// </summary>
/// <param name="mediator"></param>
public class RetrieveAddresses(IMediator mediator)
  : Endpoint<RetrieveAddressesRequest, RetrieveAddressesResponse>
{
  public override void Configure()
  {
    Get(RetrieveAddressesRequest.Route);
    Policies(AuthPolicies.RequireAuthenticatedUser);
    Summary(s =>
    {
      s.Summary = "Retrieve address data with automatic hydration";
      s.Description = "Retrieves cached address data by ProfileId and PluginId. If data is not cached, it will automatically hydrate the cache using the specified plugin and return the results. Cache stampede protection is included to prevent multiple concurrent hydration requests for the same data.";
      s.Responses[200] = "Address data retrieved successfully (either from cache or after hydration)";
      s.Responses[401] = "Unauthorized - valid JWT token required";
      s.Responses[404] = "Address data not found or plugin unable to retrieve data";
      s.Responses[400] = "Invalid request or plugin validation failed";
    });
    
    Tags("Addresses");
  }

  public override async Task HandleAsync(RetrieveAddressesRequest request, CancellationToken ct)
  {
    var query = new RetrieveAddressesQuery(request.ProfileId, 
      request.PluginId, 
      request.Provider, 
      request.Parameters);

    var result = await mediator.Send(query, ct);

    if (result.Status == ResultStatus.NotFound)
    {
      await SendNotFoundAsync(ct);
      return;
    }

    if (result.IsSuccess)
    {
      Response = new RetrieveAddressesResponse(
        result.Value.ProfileId,
        result.Value.PluginId,
        result.Value.Provider,
        result.Value.JsonData,
        result.Value.PopulatedAt);
      return;
    }

    await SendErrorsAsync(cancellation: ct);
  }
}
