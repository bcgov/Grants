using Grants.ApplicantPortal.API.UseCases;
using Grants.ApplicantPortal.API.UseCases.Contacts.SetAsPrimary;
using Grants.ApplicantPortal.API.Web.Auth;
using Grants.ApplicantPortal.API.Web.Extensions;

namespace Grants.ApplicantPortal.API.Web.Contacts;

/// <summary>
/// Set a Contact as Primary
/// </summary>
/// <remarks>
/// Sets an existing Contact as the primary contact.
/// </remarks>
public class SetAsPrimary(IMediator _mediator, IPluginCacheService _cacheService)
  : Endpoint<SetAsPrimaryContactRequest, SetAsPrimaryContactResponse>
{
  public override void Configure()
  {
    Patch(SetAsPrimaryContactRequest.Route);
    Policies(AuthPolicies.RequireAuthenticatedUser);
    Summary(s =>
    {      
      s.Summary = "Set a Contact as Primary.";
      s.Description = "Set an existing Contact as the primary contact for the profile.";
      s.Responses[200] = "Contact set as primary successfully";
      s.Responses[400] = "Bad request - validation errors";
      s.Responses[401] = "Unauthorized - authentication required";
      s.Responses[404] = "Contact, plugin, or provider not found";
      s.Responses[422] = "Unprocessable entity - invalid data";
      s.ExampleRequest = new SetAsPrimaryContactRequest 
      { 
        ContactId = Guid.NewGuid(),        
        PluginId = "DEMO",
        Provider = "PROGRAM1"
      };
    });
    
    Tags("Contacts", "Contact Management");
  }

  public override async Task HandleAsync(
    SetAsPrimaryContactRequest request,
    CancellationToken ct)
  {
    // Get the current user's profile ID from the HTTP context
    var profileId = HttpContext.GetRequiredProfileId();

    var command = new SetAsPrimaryContactCommand(
      request.ContactId,
      profileId,
      request.PluginId,
      request.Provider);

    var result = await _mediator.Send(command, ct);

    if (result.IsSuccess)
    {
      var primaryId = await PrimaryContactResolver.GetPrimaryContactIdAsync(
          _cacheService, profileId, request.PluginId, request.Provider, ct);

      Response = new SetAsPrimaryContactResponse
      {
        ContactId = request.ContactId,
        Message = "Contact set as primary successfully",
        PrimaryContactId = primaryId
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
