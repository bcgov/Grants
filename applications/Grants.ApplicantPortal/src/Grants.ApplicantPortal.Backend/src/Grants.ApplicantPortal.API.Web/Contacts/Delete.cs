using Grants.ApplicantPortal.API.UseCases.Contacts.Delete;
using Grants.ApplicantPortal.API.Web.Auth;
using Grants.ApplicantPortal.API.Web.Extensions;

namespace Grants.ApplicantPortal.API.Web.Contacts;

/// <summary>
/// Delete an existing Contact
/// </summary>
/// <remarks>
/// Deletes an existing Contact from the system.
/// </remarks>
public class Delete(IMediator _mediator)
  : Endpoint<DeleteContactRequest, DeleteContactResponse>
{
  public override void Configure()
  {
    Delete(DeleteContactRequest.Route);
    Policies(AuthPolicies.RequireAuthenticatedUser);
    Summary(s =>
    {      
      s.Summary = "Delete an existing Contact.";
      s.Description = "Delete an existing Contact from the system permanently.";
      s.Responses[200] = "Contact deleted successfully";
      s.Responses[400] = "Bad request - validation errors";
      s.Responses[401] = "Unauthorized - authentication required";
      s.Responses[404] = "Contact, plugin, or provider not found";
      s.Responses[422] = "Unprocessable entity - invalid data";
      s.ExampleRequest = new DeleteContactRequest 
      { 
        ContactId = Guid.NewGuid(),
        PluginId = "DEMO",
        Provider = "PROGRAM1"
      };
    });
    
    Tags("Contacts", "Contact Management");
  }

  public override async Task HandleAsync(
    DeleteContactRequest request,
    CancellationToken ct)
  {
    // Get the current user's profile from the HTTP context
    var profile = HttpContext.GetRequiredProfile();
    var profileId = profile.Id;

    var command = new DeleteContactCommand(
      request.ContactId,
      request.ApplicantId,
      profileId,
      request.PluginId,
      request.Provider,      
      profile.Subject);

    var result = await _mediator.Send(command, ct);

    if (result.IsSuccess)
    {
      Response = new DeleteContactResponse
      {
        ContactId = result.Value.ContactId,
        Message = "Contact deleted successfully",
        PrimaryContactId = result.Value.PrimaryContactId
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
