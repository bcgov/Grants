using Grants.ApplicantPortal.API.Core.Services;
using Grants.ApplicantPortal.API.Web.Auth;
using Grants.ApplicantPortal.API.Web.Extensions;

namespace Grants.ApplicantPortal.API.Web.Events;

public class RetrieveEventsRequest
{
  public const string Route = "/Events/{PluginId}/{Provider}";
  public string PluginId { get; set; } = string.Empty;
  public string Provider { get; set; } = string.Empty;
}

public class PluginEventDto
{
  public Guid EventId { get; set; }
  public string Severity { get; set; } = string.Empty;
  public string Source { get; set; } = string.Empty;
  public string DataType { get; set; } = string.Empty;
  public string? EntityId { get; set; }
  public string UserMessage { get; set; } = string.Empty;
  public DateTime CreatedAt { get; set; }
  public bool IsAcknowledged { get; set; }
}

public class RetrieveEventsResponse
{
  public List<PluginEventDto> Events { get; set; } = [];
}

/// <summary>
/// Retrieves unacknowledged plugin failure events for the current user.
/// </summary>
public class RetrieveEvents(IPluginEventService _eventService)
  : Endpoint<RetrieveEventsRequest, RetrieveEventsResponse>
{
  public override void Configure()
  {
    Get(RetrieveEventsRequest.Route);
    Policies(AuthPolicies.RequireAuthenticatedUser);
    Summary(s =>
    {
      s.Summary = "Retrieve active plugin events.";
      s.Description = "Returns unacknowledged failure events for the specified plugin and provider.";
      s.Responses[200] = "Events retrieved successfully";
      s.Responses[401] = "Unauthorized";
    });
    Tags("Events", "Plugin Events");
  }

  public override async Task HandleAsync(RetrieveEventsRequest request, CancellationToken ct)
  {
    var profileId = HttpContext.GetRequiredProfileId();

    var events = await _eventService.GetActiveEventsAsync(
        profileId, request.PluginId, request.Provider, ct);

    Response = new RetrieveEventsResponse
    {
      Events = events.Select(e => new PluginEventDto
      {
        EventId = e.EventId,
        Severity = e.Severity.ToString(),
        Source = e.Source.ToString(),
        DataType = e.DataType,
        EntityId = e.EntityId,
        UserMessage = e.UserMessage,
        CreatedAt = e.CreatedAt,
        IsAcknowledged = e.IsAcknowledged
      }).ToList()
    };
  }
}
