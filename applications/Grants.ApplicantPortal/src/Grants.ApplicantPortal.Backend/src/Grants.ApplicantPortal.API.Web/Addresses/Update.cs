using Grants.ApplicantPortal.API.UseCases.Addresses.Edit;
using Grants.ApplicantPortal.API.Web.Auth;
using Grants.ApplicantPortal.API.Web.Extensions;

namespace Grants.ApplicantPortal.API.Web.Addresses;

/// <summary>
/// Update an existing Address
/// </summary>
/// <remarks>
/// Updates an existing Address with new information.
/// </remarks>
public class Update(IMediator _mediator)
  : Endpoint<UpdateAddressRequest, UpdateAddressResponse>
{
  public override void Configure()
  {
    Put(UpdateAddressRequest.Route);
    Policies(AuthPolicies.RequireAuthenticatedUser);
    Summary(s =>
    {      
      s.Summary = "Update an existing Address.";
      s.Description = "Update an existing Address with new information. Valid type, address, city, province, and postal code are required.";
      s.Responses[200] = "Address updated successfully";
      s.Responses[400] = "Bad request - validation errors";
      s.Responses[401] = "Unauthorized - authentication required";
      s.Responses[404] = "Address, plugin, or provider not found";
      s.Responses[422] = "Unprocessable entity - invalid data";
      s.ExampleRequest = new UpdateAddressRequest 
      { 
        AddressId = Guid.NewGuid(),
        Type = "Business",
        Address = "123 Main Street",
        City = "Victoria",
        Province = "BC",
        PostalCode = "V8W 1A1",
        Country = "Canada",
        IsPrimary = true,        
        PluginId = "DEMO",
        Provider = "PROGRAM1"
      };
    });
    
    Tags("Addresses", "Address Management");
  }

  public override async Task HandleAsync(
    UpdateAddressRequest request,
    CancellationToken ct)
  {
    // Get the current user's profile ID from the HTTP context
    var profileId = HttpContext.GetRequiredProfileId();

    var command = new EditAddressCommand(
      request.AddressId,
      request.Type!,
      request.Address!,
      request.City!,
      request.Province!,
      request.PostalCode!,
      request.IsPrimary,
      request.Country,
      profileId,
      request.PluginId,
      request.Provider);

    var result = await _mediator.Send(command, ct);

    if (result.IsSuccess)
    {
      Response = new UpdateAddressResponse
      {
        AddressId = request.AddressId,
        Message = "Address updated successfully"
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
