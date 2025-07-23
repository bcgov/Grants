using Grants.ApplicantPortal.API.UseCases.Profiles.Retrieve;

namespace Grants.ApplicantPortal.API.Web.Profiles;

/// <summary>
/// Retrieves a profile by its unique identifier and plugin ID.
/// </summary>
/// <param name="mediator"></param>
public class RetrieveProfile(IMediator mediator)
  : Endpoint<RetrieveProfileRequest, RetrieveProfileResponse>
{
  public override void Configure()
  {
    Get(RetrieveProfileRequest.Route);
    AllowAnonymous();
    Summary(s =>
    {
      s.Summary = "Retrieve profile";
      s.Description = "Retrieves cached profile data by ProfileId and PluginId";
      s.Responses[200] = "Profile retrieved successfully";
      s.Responses[404] = "Profile not found in cache for the specified plugin";
      s.Responses[400] = "Invalid request";
    });
  }

  public override async Task HandleAsync(RetrieveProfileRequest request,
    CancellationToken ct)
  {
    var query = new RetrieveProfileQuery(request.ProfileId, request.PluginId);

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
        result.Value.JsonData,
        result.Value.PopulatedAt);
      return;
    }

    await SendErrorsAsync(cancellation: ct);
  }
}
