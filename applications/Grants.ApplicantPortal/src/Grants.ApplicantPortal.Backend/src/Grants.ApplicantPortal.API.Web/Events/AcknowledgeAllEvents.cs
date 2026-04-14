using Grants.ApplicantPortal.API.Core.Services;
using Grants.ApplicantPortal.API.Web.Auth;
using Grants.ApplicantPortal.API.Web.Extensions;

namespace Grants.ApplicantPortal.API.Web.Events;

public class AcknowledgeAllEventsRequest
{
  public const string Route = "/Events/{PluginId}/{Provider}/acknowledge-all";
  public string PluginId { get; set; } = string.Empty;
  public string Provider { get; set; } = string.Empty;
}

public class AcknowledgeAllEventsResponse
{
  public string Message { get; set; } = "All events acknowledged";
}

/// <summary>
/// Acknowledges all plugin events for the current user/plugin/provider.
/// </summary>
public class AcknowledgeAllEvents(IPluginEventService _eventService)
  : Endpoint<AcknowledgeAllEventsRequest, AcknowledgeAllEventsResponse>
{
  public override void Configure()
  {
    Patch(AcknowledgeAllEventsRequest.Route);
    Policies(AuthPolicies.RequireAuthenticatedUser);
    Summary(s =>
    {
      s.Summary = "Acknowledge all plugin events.";
      s.Description = "Marks all unacknowledged failure events for the specified plugin and provider as acknowledged.";
      s.Responses[200] = "All events acknowledged";
      s.Responses[401] = "Unauthorized";
    });
    Tags("Events", "Plugin Events");
  }

  public override async Task HandleAsync(AcknowledgeAllEventsRequest request, CancellationToken ct)
  {
    var profileId = HttpContext.GetRequiredProfileId();

    await _eventService.AcknowledgeAllAsync(
        profileId, request.PluginId, request.Provider, ct);

    Response = new AcknowledgeAllEventsResponse
    {
      Message = "All events acknowledged"
    };
  }
}
