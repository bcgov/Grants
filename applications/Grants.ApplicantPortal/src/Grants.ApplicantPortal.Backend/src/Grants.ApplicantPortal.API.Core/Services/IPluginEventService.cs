using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.Core.Services;

/// <summary>
/// Service for recording, querying, and acknowledging plugin events.
/// Events can be informational, warnings, or errors. Error events additionally
/// trigger compensation (e.g. cache invalidation).
/// </summary>
public interface IPluginEventService
{
  /// <summary>
  /// Records a plugin event. If the severity is <see cref="PluginEventSeverity.Error"/>,
  /// compensation logic (cache invalidation) is also triggered.
  /// </summary>
  Task RecordAsync(PluginEventContext context, CancellationToken cancellationToken = default);

  /// <summary>
  /// Returns unacknowledged events for a user/plugin/provider combination.
  /// </summary>
  Task<List<PluginEvent>> GetActiveEventsAsync(Guid profileId, string pluginId, string provider, CancellationToken cancellationToken = default);

  /// <summary>
  /// Acknowledges (dismisses) a single event owned by the specified profile.
  /// Returns <c>false</c> when the event does not exist or does not belong to the profile.
  /// </summary>
  Task<bool> AcknowledgeEventAsync(Guid eventId, Guid profileId, CancellationToken cancellationToken = default);

  /// <summary>
  /// Acknowledges all events for a user/plugin/provider combination.
  /// </summary>
  Task AcknowledgeAllAsync(Guid profileId, string pluginId, string provider, CancellationToken cancellationToken = default);
}

/// <summary>
/// Convenience extensions for common event patterns.
/// </summary>
public static class PluginEventServiceExtensions
{
  /// <summary>
  /// Shorthand for recording an error event with compensation.
  /// </summary>
  public static Task RecordFailureAsync(
      this IPluginEventService service,
      PluginEventContext context,
      CancellationToken cancellationToken = default)
  {
    // Ensure severity is Error regardless of what was passed
    var errorContext = context with { Severity = PluginEventSeverity.Error };
    return service.RecordAsync(errorContext, cancellationToken);
  }
}
