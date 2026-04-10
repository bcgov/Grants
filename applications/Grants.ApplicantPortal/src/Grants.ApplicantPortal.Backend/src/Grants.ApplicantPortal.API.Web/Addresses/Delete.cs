using Grants.ApplicantPortal.API.UseCases.Addresses.Delete;
using Grants.ApplicantPortal.API.Web.Auth;
using Grants.ApplicantPortal.API.Web.Extensions;

namespace Grants.ApplicantPortal.API.Web.Addresses;

/// <summary>
/// Delete an existing Address
/// </summary>
/// <remarks>
/// Deletes an existing Address from the system.
/// </remarks>
public class Delete(IMediator _mediator)
  : Endpoint<DeleteAddressRequest, DeleteAddressResponse>
{
  public override void Configure()
  {
    Delete(DeleteAddressRequest.Route);
    Policies(AuthPolicies.RequireAuthenticatedUser);
    Summary(s =>
    {      
      s.Summary = "Delete an existing Address.";
      s.Description = "Delete an existing Address from the system permanently.";
      s.Responses[200] = "Address deleted successfully";
      s.Responses[400] = "Bad request - validation errors";
      s.Responses[401] = "Unauthorized - authentication required";
      s.Responses[403] = "Forbidden - resource ownership validation failed";
      s.Responses[404] = "Address, plugin, or provider not found";
      s.Responses[422] = "Unprocessable entity - invalid data";
      s.ExampleRequest = new DeleteAddressRequest 
      { 
        AddressId = Guid.NewGuid(),
        ApplicantId = Guid.Parse("d3b07384-d9a0-4e9b-8a1f-1c2d3e4f5a6b"),
        PluginId = "DEMO",
        Provider = "PROGRAM1"
      };
    });
    
    Tags("Addresses", "Address Management");
  }

  public override async Task HandleAsync(
    DeleteAddressRequest request,
    CancellationToken ct)
  {
    // Get the current user's profile from the HTTP context
    var profile = HttpContext.GetRequiredProfile();
    var profileId = profile.Id;

    var command = new DeleteAddressCommand(
      request.AddressId,
      request.ApplicantId,
      profileId,
      request.PluginId,
      request.Provider,      
      profile.Subject);

    var result = await _mediator.Send(command, ct);

    if (result.IsSuccess)
    {
      Response = new DeleteAddressResponse
      {
        AddressId = result.Value.AddressId,
        Message = "Address deleted successfully",
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
