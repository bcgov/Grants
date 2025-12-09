using Grants.ApplicantPortal.API.UseCases.Contacts.Delete;
using Grants.ApplicantPortal.API.Web.Auth;

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
        ProfileId = Guid.NewGuid(),
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
    var command = new DeleteContactCommand(
      request.ContactId,
      request.ProfileId,
      request.PluginId,
      request.Provider);

    var result = await _mediator.Send(command, ct);

    if (result.IsSuccess)
    {
      Response = new DeleteContactResponse
      {
        ContactId = request.ContactId,
        Message = "Contact deleted successfully"
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
