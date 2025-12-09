using Grants.ApplicantPortal.API.UseCases.Contacts.Create;
using Grants.ApplicantPortal.API.Web.Auth;

namespace Grants.ApplicantPortal.API.Web.Contacts;

/// <summary>
/// Create a new Contact
/// </summary>
/// <remarks>
/// Creates a new Contact given a name and other details.
/// </remarks>
public class Create(IMediator _mediator)
  : Endpoint<CreateContactRequest, CreateContactResponse>
{
  public override void Configure()
  {
    Post(CreateContactRequest.Route);
    Policies(AuthPolicies.RequireAuthenticatedUser);
    Summary(s =>
    {      
      s.Summary = "Create a new Contact.";
      s.Description = "Create a new Contact with the specified details. Valid name and type are required.";
      s.Responses[201] = "Contact created successfully";
      s.Responses[400] = "Bad request - validation errors";
      s.Responses[401] = "Unauthorized - authentication required";
      s.Responses[404] = "Plugin or provider not found";
      s.Responses[422] = "Unprocessable entity - invalid data";
      s.ExampleRequest = new CreateContactRequest 
      { 
        Name = "John Doe",
        Email = "john.doe@example.com",
        IsPrimary = true,
        PhoneNumber = "123-456-7890",
        Title = "Manager",
        Type = "Business",
        ProfileId = Guid.NewGuid(),
        PluginId = "DEMO",
        Provider = "PROGRAM1"
      };
    });
    
    Tags("Contacts", "Contact Management");
  }

  public override async Task HandleAsync(
    CreateContactRequest request,
    CancellationToken ct)
  {
    var command = new CreateContactCommand(
      request.Name!,
      request.Type!,
      request.IsPrimary,
      request.Title,
      request.Email,
      request.PhoneNumber,
      request.ProfileId,
      request.PluginId,
      request.Provider);

    var result = await _mediator.Send(command, ct);

    if (result.IsSuccess)
    {
      Response = new CreateContactResponse
      {
        ContactId = result.Value,
        Name = request.Name!
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
