using Grants.ApplicantPortal.API.UseCases.Contributors.Get;

namespace Grants.ApplicantPortal.API.Web.Contributors;

/// <summary>
/// Get a Contributor by integer ID.
/// </summary>
/// <remarks>
/// Takes a positive integer ID and returns a matching Contributor record.
/// </remarks>
public class GetById(IMediator _mediator)
  : Endpoint<GetContributorByIdRequest, ContributorRecord>
{
  public override void Configure()
  {
    Get(GetContributorByIdRequest.Route);
    AllowAnonymous();
  }

  public override async Task HandleAsync(GetContributorByIdRequest request,
    CancellationToken ct)
  {
    var query = new GetContributorQuery(request.ContributorId);

    var result = await _mediator.Send(query, ct);

    if (result.Status == ResultStatus.NotFound)
    {
      await SendNotFoundAsync(ct);
      return;
    }

    if (result.IsSuccess)
    {
      Response = new ContributorRecord(result.Value.Id, result.Value.Name, result.Value.PhoneNumber);
    }
  }
}
