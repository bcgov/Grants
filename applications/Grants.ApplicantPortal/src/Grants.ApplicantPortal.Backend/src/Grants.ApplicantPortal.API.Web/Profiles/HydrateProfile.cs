using Grants.ApplicantPortal.API.UseCases.Profiles.Hydrate;
using Grants.ApplicantPortal.API.Web.Auth;

namespace Grants.ApplicantPortal.API.Web.Profiles;

/// <summary>
/// Hydrates profile data cache using the specified plugin.
/// </summary>
/// <param name="mediator"></param>
public class HydrateProfile(IMediator mediator)
  : Endpoint<HydrateProfileRequest, HydrateProfileResponse>
{
  public override void Configure()
  {
    Post(HydrateProfileRequest.Route);
    Policies(AuthPolicies.RequireAuthenticatedUser); // Require authenticated user
    Summary(s =>
    {
      s.Summary = "Hydrate profile data cache using a plugin";
      s.Description = "Hydrates the Redis cache with profile data using the specified plugin and additional data";
      s.Responses[200] = "Profile data hydrated and cached successfully";
      s.Responses[401] = "Unauthorized - valid JWT token required";
      s.Responses[400] = "Invalid request or plugin not found";
      s.Responses[404] = "Profile or plugin not found";
    });
  }

  public override async Task HandleAsync(HydrateProfileRequest request,
    CancellationToken ct)
  {       
    var command = new HydrateProfileCommand(
      request.ProfileId, 
      request.PluginId, 
      request.AdditionalData);

    var result = await mediator.Send(command, ct);

    if (result.Status == ResultStatus.NotFound)
    {
      await SendNotFoundAsync(ct);
      return;
    }

    if (result.Status == ResultStatus.Invalid)
    {
      await SendErrorsAsync(cancellation: ct);
      return;
    }

    if (result.IsSuccess)
    {
      var profileData = result.Value;
      Response = new HydrateProfileResponse(
        profileData.ProfileId,
        profileData.PluginId,
        profileData.JsonData,
        profileData.PopulatedAt        
      );
      return;
    }

    await SendErrorsAsync(cancellation: ct);
  }
}
