using Grants.ApplicantPortal.API.UseCases.Addresses.Create;
using Grants.ApplicantPortal.API.Web.Auth;
using Grants.ApplicantPortal.API.Web.Extensions;

namespace Grants.ApplicantPortal.API.Web.Addresses;

/// <summary>
/// Create a new Address
/// </summary>
/// <remarks>
/// Creates a new Address given a type and other details.
/// </remarks>
public class Create(IMediator _mediator)
  : Endpoint<CreateAddressRequest, CreateAddressResponse>
{
  public override void Configure()
  {
    Post(CreateAddressRequest.Route);
    Policies(AuthPolicies.RequireAuthenticatedUser);
    Summary(s =>
    {      
      s.Summary = "Create a new Address.";
      s.Description = "Create a new Address with the specified details. Valid type, street, city, province, and postal code are required.";
      s.Responses[201] = "Address created successfully";
      s.Responses[400] = "Bad request - validation errors";
      s.Responses[401] = "Unauthorized - authentication required";
      s.Responses[403] = "Forbidden - resource ownership validation failed";
      s.Responses[404] = "Plugin or provider not found";
      s.Responses[422] = "Unprocessable entity - invalid data";
      s.ExampleRequest = new CreateAddressRequest 
      { 
        AddressType = "Physical",
        Street = "123 Main Street",
        Street2 = "Suite 100",
        Unit = "",
        City = "Victoria",
        Province = "BC",
        PostalCode = "V8W1A1",
        Country = "",
        IsPrimary = false,
        PluginId = "DEMO",
        Provider = "PROGRAM1"
      };
    });

    Tags("Addresses", "Address Management");
  }

  public override async Task HandleAsync(
    CreateAddressRequest request,
    CancellationToken ct)
  {
    var profile = HttpContext.GetRequiredProfile();
    var profileId = profile.Id;

    var command = new CreateAddressCommand(
      request.AddressType!,
      request.Street!,
      request.City!,
      request.Province!,
      request.PostalCode!,
      request.IsPrimary,
      request.Street2,
      request.Unit,
      request.Country,
      profileId,
      request.PluginId,
      request.Provider,
      request.ApplicantId,
      profile.Subject);

    var result = await _mediator.Send(command, ct);

    if (result.IsSuccess)
    {
      Response = new CreateAddressResponse
      {
        AddressId = result.Value.AddressId,
        PrimaryAddressId = result.Value.PrimaryAddressId
      };
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
