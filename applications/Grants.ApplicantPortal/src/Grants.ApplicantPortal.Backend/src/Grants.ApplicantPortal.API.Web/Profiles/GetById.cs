using Grants.ApplicantPortal.API.UseCases.Profiles.Get;

namespace Grants.ApplicantPortal.API.Web.Profiles;

/// <summary>
/// Gets a profile by its unique identifier.
/// </summary>
/// <param name="_mediator"></param>
public class GetById(IMediator _mediator)
  : Endpoint<GetProfileByIdRequest, ProfileRecord>
{
  public override void Configure()
  {
    Get(GetProfileByIdRequest.Route);
    AllowAnonymous();
  }

  public override async Task HandleAsync(GetProfileByIdRequest request,
    CancellationToken ct)
  {
    var query = new GetProfileQuery(request.ProfileId);

    var result = await _mediator.Send(query, ct);

    if (result.Status == ResultStatus.NotFound)
    {
      await SendNotFoundAsync(ct);
      return;
    }

    if (result.IsSuccess)
    {
      Response = new ProfileRecord(result.Value.Id, result.Value.Profile);
    }
  }
}

