using System.Threading;
using Grants.ApplicantPortal.API.UseCases.Contributors.Delete;

namespace Grants.ApplicantPortal.API.Web.Contributors;

/// <summary>
/// Delete a Contributor.
/// </summary>
/// <remarks>
/// Delete a Contributor by providing a valid integer id.
/// </remarks>
public class Delete(IMediator _mediator)
  : Endpoint<DeleteContributorRequest>
{
  public override void Configure()
  {
    Delete(DeleteContributorRequest.Route);
    AllowAnonymous();
  }

  public override async Task HandleAsync(
    DeleteContributorRequest request,
    CancellationToken ct)
  {
    var command = new DeleteContributorCommand(request.ContributorId);

    var result = await _mediator.Send(command, ct);

    if (result.Status == ResultStatus.NotFound)
    {
      await SendNotFoundAsync(ct);
      return;
    }

    if (result.IsSuccess)
    {
      await SendNoContentAsync(ct);
    }

    // TODO: Handle other issues as needed
  }
}
