using Grants.ApplicantPortal.API.UseCases.ProfileConfigurations.Update;

namespace Grants.ApplicantPortal.API.Web.PluginConfigurations;

public class Update(IMediator _mediator)
  : Endpoint<UpdatePluginConfigurationRequest, UpdatePluginConfigurationResponse>
{
  public override void Configure()
  {
    Put(UpdatePluginConfigurationRequest.Route);
    AllowAnonymous();
    Summary(s =>
    {
      Summary(s =>
      {
        s.Summary = "Update plugin configuration";
        s.Description = "Updated plugin configuration record";
        s.Responses[200] = "Plugin configuration created successfully";
        s.Responses[401] = "Unauthorized - valid JWT token required";
        s.Responses[400] = "Invalid request or plugin configuration creation failed";
      });
    });
  }

  public override async Task HandleAsync(
    UpdatePluginConfigurationRequest request,
    CancellationToken ct)
  {
    var result = await _mediator.Send(new UpdatePluginConfigurationCommand(), ct);

    if (result.IsSuccess)
    {
      Response = new UpdatePluginConfigurationResponse();
    }
  }
}
