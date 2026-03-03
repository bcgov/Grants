using Grants.ApplicantPortal.API.UseCases.Organizations.Edit;
using Grants.ApplicantPortal.API.Web.Auth;
using Grants.ApplicantPortal.API.Web.Extensions;

namespace Grants.ApplicantPortal.API.Web.Organizations;

/// <summary>
/// Update an existing Organization
/// </summary>
/// <remarks>
/// Updates an existing Organization with new information.
/// </remarks>
public class Update(IMediator _mediator)
  : Endpoint<UpdateOrganizationRequest, UpdateOrganizationResponse>
{
  public override void Configure()
  {
    Put(UpdateOrganizationRequest.Route);
    Policies(AuthPolicies.RequireAuthenticatedUser);
    Summary(s =>
    {      
      s.Summary = "Update an existing Organization.";
      s.Description = "Update an existing Organization with new information. Valid name, organization type, organization number, and status are required.";
      s.Responses[200] = "Organization updated successfully";
      s.Responses[400] = "Bad request - validation errors";
      s.Responses[401] = "Unauthorized - authentication required";
      s.Responses[404] = "Organization, plugin, or provider not found";
      s.Responses[422] = "Unprocessable entity - invalid data";
      s.ExampleRequest = new UpdateOrganizationRequest 
      { 
        OrganizationId = Guid.NewGuid(),
        Name = "Updated Organization Name Inc.",
        OrganizationType = "Corporation",
        OrganizationNumber = "BC1234567",
        Status = "Active",
        NonRegOrgName = "Updated Organization Legal Name Inc.",        
        FiscalMonth = "December",
        FiscalDay = 31,                
        PluginId = "DEMO",
        Provider = "PROGRAM1",
        OrganizationSize = 500
      };
    });
    
    Tags("Organizations", "Organization Management");
  }

  public override async Task HandleAsync(
    UpdateOrganizationRequest request,
    CancellationToken ct)
  {
    // Get the current user's profile ID from the HTTP context
    var profileId = HttpContext.GetRequiredProfileId();

    var command = new EditOrganizationCommand(
      request.OrganizationId,
      request.Name!,
      request.OrganizationType!,
      request.OrganizationNumber!,
      request.Status!,
      request.NonRegOrgName,
      request.FiscalMonth,
      request.FiscalDay,
      request.OrganizationSize,
      profileId,
      request.PluginId,
      request.Provider);

    var result = await _mediator.Send(command, ct);

    if (result.IsSuccess)
    {
      Response = new UpdateOrganizationResponse
      {
        OrganizationId = request.OrganizationId,
        Message = "Organization updated successfully"
      };
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

    // Handle other error cases
    if (result.Errors.Any())
    {
      foreach (var error in result.Errors)
      {
        AddError(error);
      }
      await SendErrorsAsync(400, ct);
    }
  }
}
