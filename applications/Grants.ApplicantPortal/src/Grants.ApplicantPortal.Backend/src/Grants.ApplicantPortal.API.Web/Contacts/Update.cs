using Grants.ApplicantPortal.API.UseCases.Contacts.Edit;
using Grants.ApplicantPortal.API.Web.Auth;
using Grants.ApplicantPortal.API.Web.Extensions;

namespace Grants.ApplicantPortal.API.Web.Contacts;

/// <summary>
/// Update an existing Contact
/// </summary>
/// <remarks>
/// Updates an existing Contact with new information.
/// </remarks>
public class Update(IMediator _mediator)
  : Endpoint<UpdateContactRequest, UpdateContactResponse>
{
  public override void Configure()
  {
    Put(UpdateContactRequest.Route);
    Policies(AuthPolicies.RequireAuthenticatedUser);
    Summary(s =>
    {      
      s.Summary = "Update an existing Contact.";
      s.Description = "Update an existing Contact with new information. Valid name and type are required.";
      s.Responses[200] = "Contact updated successfully";
      s.Responses[400] = "Bad request - validation errors";
      s.Responses[401] = "Unauthorized - authentication required";
      s.Responses[404] = "Contact, plugin, or provider not found";
      s.Responses[422] = "Unprocessable entity - invalid data";
      s.ExampleRequest = new UpdateContactRequest 
      { 
        ContactId = Guid.NewGuid(),
        Name = "John Doe Updated",
        Email = "john.doe.updated@example.com",
        IsPrimary = false,
        PhoneNumber = "987-654-3210",
        Title = "Senior Manager",
        Type = "Business",        
        PluginId = "DEMO",
        Provider = "PROGRAM1"
      };
    });
    
    Tags("Contacts", "Contact Management");
  }

  public override async Task HandleAsync(
    UpdateContactRequest request,
    CancellationToken ct)
  {
    // Get the current user's profile ID from the HTTP context
    var profileId = HttpContext.GetRequiredProfileId();

    var command = new EditContactCommand(
      request.ContactId,
      request.Name!,
      request.Type!,
      request.IsPrimary,
      request.Title,
      request.Email,
      request.PhoneNumber,
      profileId,
      request.PluginId,
      request.Provider);

    var result = await _mediator.Send(command, ct);

    if (result.IsSuccess)
    {
      Response = new UpdateContactResponse
      {
        ContactId = request.ContactId,
        Message = "Contact updated successfully"
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
