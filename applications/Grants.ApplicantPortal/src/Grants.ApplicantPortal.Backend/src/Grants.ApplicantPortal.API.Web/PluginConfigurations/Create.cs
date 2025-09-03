using System.Text.Json;
using Grants.ApplicantPortal.API.UseCases.ProfileConfigurations.Create;

namespace Grants.ApplicantPortal.API.Web.PluginConfigurations;

/// <summary>
/// Create a new Plugin Configuration
/// </summary>
public class Create(IMediator _mediator)
  : Endpoint<CreatePluginConfigurationRequest, CreatePluginConfigurationResponse>
{
  public override void Configure()
  {
    Post(CreatePluginConfigurationRequest.Route);
    AllowAnonymous();
    Summary(s =>
    {
      Summary(s =>
      {
        s.Summary = "Create plugin configuration";
        s.Description = "Creates plugin configuration record";
        s.Responses[200] = "Plugin configuration created successfully";
        s.Responses[401] = "Unauthorized - valid JWT token required";        
        s.Responses[400] = "Invalid request or plugin configuration creation failed";        
      });
    });
  }

  public override async Task HandleAsync(
    CreatePluginConfigurationRequest request,
    CancellationToken ct)
  {
    var result = await _mediator.Send(new CreatePluginConfigurationCommand(), ct);

    if (result.IsSuccess)
    {
      Response = new CreatePluginConfigurationResponse();
    }    
  }
}
