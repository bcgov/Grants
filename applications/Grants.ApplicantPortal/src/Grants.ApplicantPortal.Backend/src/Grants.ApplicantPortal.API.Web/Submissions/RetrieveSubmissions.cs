using Grants.ApplicantPortal.API.UseCases.Submissions.Retrieve;
using Grants.ApplicantPortal.API.Web.Auth;
using Grants.ApplicantPortal.API.Web.Extensions;

namespace Grants.ApplicantPortal.API.Web.Submissions;

/// <summary>
/// Retrieves submission data for a profile by its unique identifier and plugin ID, with automatic cache hydration.
/// </summary>
/// <param name="mediator"></param>
public class RetrieveSubmissions(IMediator mediator)
  : Endpoint<RetrieveSubmissionsRequest, RetrieveSubmissionsResponse>
{
  public override void Configure()
  {
    Get(RetrieveSubmissionsRequest.Route);
    Policies(AuthPolicies.RequireAuthenticatedUser);
    Summary(s =>
    {
      s.Summary = "Retrieve submission data with automatic hydration";
      s.Description = "Retrieves cached submission data by ProfileId and PluginId. If data is not cached, it will automatically hydrate the cache using the specified plugin and return the results. Cache stampede protection is included to prevent multiple concurrent hydration requests for the same data.";
      s.Responses[200] = "Submission data retrieved successfully (either from cache or after hydration)";
      s.Responses[401] = "Unauthorized - valid JWT token required";
      s.Responses[404] = "Submission data not found or plugin unable to retrieve data";
      s.Responses[400] = "Invalid request or plugin validation failed";
    });
    
    Tags("Submissions");
  }

  public override async Task HandleAsync(RetrieveSubmissionsRequest request, CancellationToken ct)
  {
    // Get the current user's profile ID from the HTTP context
    var profileId = HttpContext.GetRequiredProfileId();

    var query = new RetrieveSubmissionsQuery(profileId, 
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
      Response = new RetrieveSubmissionsResponse(
        result.Value.ProfileId,
        result.Value.PluginId,
        result.Value.Provider,
        result.Value.Data,
        result.Value.PopulatedAt);
      return;
    }

    await SendErrorsAsync(cancellation: ct);
  }
}
