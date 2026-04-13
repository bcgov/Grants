using Grants.ApplicantPortal.API.UseCases.Addresses.SetAsPrimary;
using Grants.ApplicantPortal.API.Web.Auth;
using Grants.ApplicantPortal.API.Web.Extensions;

namespace Grants.ApplicantPortal.API.Web.Addresses;

/// <summary>
/// Set an Address as Primary
/// </summary>
/// <remarks>
/// Sets an existing Address as the primary address.
/// </remarks>
public class SetAsPrimary(IMediator _mediator)
  : Endpoint<SetAsPrimaryAddressRequest, SetAsPrimaryAddressResponse>
{
  public override void Configure()
  {
    Patch(SetAsPrimaryAddressRequest.Route);
    Policies(AuthPolicies.RequireAuthenticatedUser);
    Summary(s =>
    {      
      s.Summary = "Set an Address as Primary.";
      s.Description = "Set an existing Address as the primary address for the profile.";
      s.Responses[200] = "Address set as primary successfully";
      s.Responses[400] = "Bad request - validation errors";
      s.Responses[401] = "Unauthorized - authentication required";
      s.Responses[403] = "Forbidden - resource ownership validation failed";
      s.Responses[404] = "Address, plugin, or provider not found";
      s.Responses[422] = "Unprocessable entity - invalid data";
      s.ExampleRequest = new SetAsPrimaryAddressRequest 
      { 
        AddressId = Guid.NewGuid(),
        PluginId = "DEMO",
        Provider = "PROGRAM1"
      };
    });
    
    Tags("Addresses", "Address Management");
  }

  public override async Task HandleAsync(
    SetAsPrimaryAddressRequest request,
    CancellationToken ct)
  {
    // Get the current user's profile from the HTTP context
    var profile = HttpContext.GetRequiredProfile();
    var profileId = profile.Id;

    var command = new SetAsPrimaryAddressCommand(
      request.AddressId,
      profileId,
      request.PluginId,
      request.Provider,
      profile.Subject);

    var result = await _mediator.Send(command, ct);

    if (result.IsSuccess)
    {
      Response = new SetAsPrimaryAddressResponse
      {
        AddressId = result.Value.AddressId,
        Message = "Address set as primary successfully",
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
