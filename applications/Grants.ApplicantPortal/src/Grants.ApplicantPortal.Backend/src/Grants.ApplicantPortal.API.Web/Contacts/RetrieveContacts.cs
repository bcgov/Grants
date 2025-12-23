using Grants.ApplicantPortal.API.UseCases.Contacts.Retrieve;
using Grants.ApplicantPortal.API.Web.Auth;
using Grants.ApplicantPortal.API.Web.Extensions;

namespace Grants.ApplicantPortal.API.Web.Contacts;

/// <summary>
/// Retrieves contact data for a profile by its unique identifier and plugin ID, with automatic cache hydration.
/// </summary>
/// <param name="mediator"></param>
public class RetrieveContacts(IMediator mediator)
  : Endpoint<RetrieveContactsRequest, RetrieveContactsResponse>
{
  public override void Configure()
  {
    Get(RetrieveContactsRequest.Route);
    Policies(AuthPolicies.RequireAuthenticatedUser);
    Summary(s =>
    {
      s.Summary = "Retrieve contact data with automatic hydration";
      s.Description = "Retrieves cached contact data by ProfileId and PluginId. If data is not cached, it will automatically hydrate the cache using the specified plugin and return the results. Cache stampede protection is included to prevent multiple concurrent hydration requests for the same data.";
      s.Responses[200] = "Contact data retrieved successfully (either from cache or after hydration)";
      s.Responses[401] = "Unauthorized - valid JWT token required";
      s.Responses[404] = "Contact data not found or plugin unable to retrieve data";
      s.Responses[400] = "Invalid request or plugin validation failed";
    });

    // Add tags for better organization
    Tags("Contacts");
  }

  public override async Task HandleAsync(RetrieveContactsRequest request,
    CancellationToken ct)
  {
    // Get the current user's profile ID from the HTTP context
    var profileId = HttpContext.GetRequiredProfileId();

    var query = new RetrieveContactsQuery(profileId,
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
      Response = new RetrieveContactsResponse(
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
