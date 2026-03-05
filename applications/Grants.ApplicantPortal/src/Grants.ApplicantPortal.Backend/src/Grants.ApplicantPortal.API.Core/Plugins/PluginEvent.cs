namespace Grants.ApplicantPortal.API.Core.Plugins;

/// <summary>
/// Severity of a plugin event
/// </summary>
public enum PluginEventSeverity
{
  Info = 0,
  Warning = 1,
  Error = 2
}

/// <summary>
/// Source that triggered the plugin event
/// </summary>
public enum PluginEventSource
{
  /// <summary>
  /// Outbox message failed to publish after max retries
  /// </summary>
  OutboxFailure = 0,

  /// <summary>
  /// External system returned a FAILED acknowledgment
  /// </summary>
  InboxRejection = 1
}

/// <summary>
/// Represents a failure event surfaced to the user when an async command fails.
/// Stored in the database so events persist across sessions.
/// </summary>
public class PluginEvent
{
  private PluginEvent() { }

  public PluginEvent(
      Guid profileId,
      string pluginId,
      string provider,
      string dataType,
      string? entityId,
      PluginEventSeverity severity,
      PluginEventSource source,
      string userMessage,
      string? technicalDetails = null,
      Guid? originalMessageId = null,
      string? correlationId = null)
  {
    EventId = Guid.NewGuid();
    ProfileId = profileId;
    PluginId = pluginId;
    Provider = provider;
    DataType = dataType;
    EntityId = entityId;
    Severity = severity;
    Source = source;
    UserMessage = userMessage;
    TechnicalDetails = technicalDetails;
    OriginalMessageId = originalMessageId;
    CorrelationId = correlationId;
    IsAcknowledged = false;
    CreatedAt = DateTime.UtcNow;
  }

  public long Id { get; private set; }
  public Guid EventId { get; private set; }
  public Guid ProfileId { get; private set; }
  public string PluginId { get; private set; } = null!;
  public string Provider { get; private set; } = null!;
  public string DataType { get; private set; } = null!;
  public string? EntityId { get; private set; }
  public PluginEventSeverity Severity { get; private set; }
  public PluginEventSource Source { get; private set; }
  public string UserMessage { get; private set; } = null!;
  public string? TechnicalDetails { get; private set; }
  public Guid? OriginalMessageId { get; private set; }
  public string? CorrelationId { get; private set; }
  public bool IsAcknowledged { get; private set; }
  public DateTime CreatedAt { get; private set; }
  public DateTime? AcknowledgedAt { get; private set; }

  public void Acknowledge()
  {
    IsAcknowledged = true;
    AcknowledgedAt = DateTime.UtcNow;
  }
}
