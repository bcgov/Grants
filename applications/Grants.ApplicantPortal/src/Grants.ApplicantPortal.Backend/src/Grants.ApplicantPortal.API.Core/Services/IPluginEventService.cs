using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.Core.Services;

/// <summary>
/// Service for recording, querying, and acknowledging plugin failure events.
/// When a failure is recorded the relevant cache segment is invalidated (compensation)
/// so the next user read fetches fresh data from the external API.
/// </summary>
public interface IPluginEventService
{
  /// <summary>
  /// Records a failure event and invalidates the relevant cache segment.
  /// </summary>
  Task RecordFailureAsync(PluginFailureContext context, CancellationToken cancellationToken = default);

  /// <summary>
  /// Returns unacknowledged events for a user/plugin/provider combination.
  /// </summary>
  Task<List<PluginEvent>> GetActiveEventsAsync(Guid profileId, string pluginId, string provider, CancellationToken cancellationToken = default);

  /// <summary>
  /// Acknowledges (dismisses) a single event.
  /// </summary>
  Task AcknowledgeEventAsync(Guid eventId, CancellationToken cancellationToken = default);

  /// <summary>
  /// Acknowledges all events for a user/plugin/provider combination.
  /// </summary>
  Task AcknowledgeAllAsync(Guid profileId, string pluginId, string provider, CancellationToken cancellationToken = default);
}
