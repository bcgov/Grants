using Grants.ApplicantPortal.API.UseCases.Profiles.Retrieve;

namespace Grants.ApplicantPortal.API.Web.Profiles;

/// <summary>
/// Retrieves a profile by its unique identifier and plugin ID, with automatic cache hydration.
/// </summary>
/// <param name="mediator"></param>
public class RetrieveProfile(IMediator mediator)
  : Endpoint<RetrieveProfileRequest, RetrieveProfileResponse>
{
  public override void Configure()
  {
    Get(RetrieveProfileRequest.Route);
    //Policies(AuthPolicies.RequireAuthenticatedUser); // Require authenticated user
    AllowAnonymous(); // Allow anonymous access for testing purposes
    Summary(s =>
    {
      s.Summary = "Retrieve profile with automatic hydration";
      s.Description = "Retrieves cached profile data by ProfileId and PluginId. If data is not cached, it will automatically hydrate the cache using the specified plugin and return the results. Cache stampede protection is included to prevent multiple concurrent hydration requests for the same data.";
      s.Responses[200] = "Profile retrieved successfully (either from cache or after hydration)";
      s.Responses[401] = "Unauthorized - valid JWT token required";
      s.Responses[404] = "Profile not found or plugin unable to retrieve data";
      s.Responses[400] = "Invalid request or plugin validation failed";
    });
  }

  public override async Task HandleAsync(RetrieveProfileRequest request,
    CancellationToken ct)
  {
    var query = new RetrieveProfileQuery(request.ProfileId, request.PluginId, request.Provider, request.Key, request.AdditionalData);

    var result = await mediator.Send(query, ct);

    if (result.Status == ResultStatus.NotFound)
    {
      await SendNotFoundAsync(ct);
      return;
    }

    if (result.IsSuccess)
    {
      Response = new RetrieveProfileResponse(
        result.Value.ProfileId,
        result.Value.PluginId,
        result.Value.Provider,
        result.Value.Key,
        result.Value.JsonData,
        result.Value.PopulatedAt);
      return;
    }

    await SendErrorsAsync(cancellation: ct);
  }
}
