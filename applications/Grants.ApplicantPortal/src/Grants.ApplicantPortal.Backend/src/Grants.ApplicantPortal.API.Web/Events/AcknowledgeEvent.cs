using Grants.ApplicantPortal.API.Core.Services;
using Grants.ApplicantPortal.API.Web.Auth;
using Grants.ApplicantPortal.API.Web.Extensions;

namespace Grants.ApplicantPortal.API.Web.Events;

public class AcknowledgeEventRequest
{
  public const string Route = "/Events/{EventId}/acknowledge";
  public Guid EventId { get; set; }
}

public class AcknowledgeEventResponse
{
  public Guid EventId { get; set; }
  public string Message { get; set; } = "Event acknowledged";
}

/// <summary>
/// Acknowledges (dismisses) a single plugin event.
/// </summary>
public class AcknowledgeEvent(IPluginEventService _eventService)
  : Endpoint<AcknowledgeEventRequest, AcknowledgeEventResponse>
{
  public override void Configure()
  {
    Patch(AcknowledgeEventRequest.Route);
    Policies(AuthPolicies.RequireAuthenticatedUser);
    Summary(s =>
    {
      s.Summary = "Acknowledge a plugin event.";
      s.Description = "Marks a plugin failure event as acknowledged (dismissed by the user).";
      s.Responses[200] = "Event acknowledged";
      s.Responses[401] = "Unauthorized";
      s.Responses[404] = "Event not found or not owned by the current user";
    });
    Tags("Events", "Plugin Events");
  }

  public override async Task HandleAsync(AcknowledgeEventRequest request, CancellationToken ct)
  {
    var profileId = HttpContext.GetRequiredProfileId();

    var acknowledged = await _eventService.AcknowledgeEventAsync(request.EventId, profileId, ct);

    if (!acknowledged)
    {
      await SendNotFoundAsync(ct);
      return;
    }

    Response = new AcknowledgeEventResponse
    {
      EventId = request.EventId,
      Message = "Event acknowledged"
    };
  }
}
