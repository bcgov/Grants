using Grants.ApplicantPortal.API.UseCases.Contributors.Get;
using Grants.ApplicantPortal.API.UseCases.Contributors.Update;

namespace Grants.ApplicantPortal.API.Web.Contributors;

/// <summary>
/// Update an existing Contributor.
/// </summary>
/// <remarks>
/// Update an existing Contributor by providing a fully defined replacement set of values.
/// See: https://stackoverflow.com/questions/60761955/rest-update-best-practice-put-collection-id-without-id-in-body-vs-put-collecti
/// </remarks>
public class Update(IMediator _mediator)
  : Endpoint<UpdateContributorRequest, UpdateContributorResponse>
{
  public override void Configure()
  {
    Put(UpdateContributorRequest.Route);
    AllowAnonymous();
  }

  public override async Task HandleAsync(
    UpdateContributorRequest request,
    CancellationToken ct)
  {
    var result = await _mediator.Send(new UpdateContributorCommand(request.Id, request.Name!), ct);

    if (result.Status == ResultStatus.NotFound)
    {
      await SendNotFoundAsync(ct);
      return;
    }

    var query = new GetContributorQuery(request.ContributorId);

    var queryResult = await _mediator.Send(query, ct);

    if (queryResult.Status == ResultStatus.NotFound)
    {
      await SendNotFoundAsync(ct);
      return;
    }

    if (queryResult.IsSuccess)
    {
      var dto = queryResult.Value;
      Response = new UpdateContributorResponse(new ContributorRecord(dto.Id, dto.Name, dto.PhoneNumber));
      return;
    }
  }
}
