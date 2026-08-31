using Grants.ApplicantPortal.API.UseCases.Submissions.RetrieveForm;
using Grants.ApplicantPortal.API.Web.Auth;
using Grants.ApplicantPortal.API.Web.Extensions;

namespace Grants.ApplicantPortal.API.Web.Submissions;

/// <summary>
/// Retrieves the form.io schema and submission data for a single submission, with
/// on-demand cache hydration (only triggered when this endpoint is invoked).
/// </summary>
/// <param name="mediator"></param>
public class RetrieveSubmissionForm(IMediator mediator)
  : Endpoint<RetrieveSubmissionFormRequest, RetrieveSubmissionFormResponse>
{
  public override void Configure()
  {
    Get(RetrieveSubmissionFormRequest.Route);
    Policies(AuthPolicies.RequireAuthenticatedUser);
    Summary(s =>
    {
      s.Summary = "Retrieve a single submission's form.io schema and data";
      s.Description = "Retrieves the form.io form definition and submission data for a single submission, for client-side rendering and PDF rasterization. If not cached, hydrates on-demand using the specified plugin. Cache stampede protection is included to prevent multiple concurrent hydration requests for the same submission.";
      s.Responses[200] = "Submission form data retrieved successfully (either from cache or after hydration)";
      s.Responses[400] = "Invalid request or plugin validation failed";
      s.Responses[401] = "Unauthorized - valid JWT token required";
      s.Responses[403] = "Forbidden - resource ownership validation failed";
      s.Responses[404] = "Submission form data not found or plugin unable to retrieve data";
      s.Responses[422] = "Unprocessable entity - invalid data";
    });

    Tags("Submissions");
  }

  public override async Task HandleAsync(RetrieveSubmissionFormRequest request, CancellationToken ct)
  {
    // Get the current user's profile ID from the HTTP context
    var profileId = HttpContext.GetRequiredProfileId();
    var subject = HttpContext.User.GetSubject() ?? string.Empty;

    var query = new RetrieveSubmissionFormQuery(
      profileId,
      request.PluginId,
      request.Provider,
      request.SubmissionId,
      subject);

    var result = await mediator.Send(query, ct);

    if (result.IsSuccess)
    {
      Response = new RetrieveSubmissionFormResponse(
        result.Value.ProfileId,
        result.Value.PluginId,
        result.Value.Provider,
        request.SubmissionId,
        result.Value.Data,
        result.Value.PopulatedAt,
        result.Value.CacheStatus,
        result.Value.CacheStore);
      return;
    }

    if (result.Status == ResultStatus.Forbidden)
    {
      await SendForbiddenAsync(ct);
      return;
    }

    if (result.Status == ResultStatus.NotFound)
    {
      await SendNotFoundAsync(ct);
      return;
    }

    if (result.Status == ResultStatus.Invalid)
    {
      foreach (var error in result.ValidationErrors)
      {
        AddError(error.ErrorMessage);
      }
      await SendErrorsAsync(422, ct);
      return;
    }

    foreach (var error in result.Errors)
    {
      AddError(error);
    }
    await SendErrorsAsync(cancellation: ct);
  }
}
