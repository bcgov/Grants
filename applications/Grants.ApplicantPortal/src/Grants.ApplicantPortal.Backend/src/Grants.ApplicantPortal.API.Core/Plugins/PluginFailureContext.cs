namespace Grants.ApplicantPortal.API.Core.Plugins;

/// <summary>
/// Context for recording a plugin failure event.
/// Encapsulates everything needed to persist the event and compensate.
/// </summary>
public record PluginFailureContext(
    Guid ProfileId,
    string PluginId,
    string Provider,
    string DataType,
    string? EntityId,
    string UserMessage,
    string? TechnicalDetails,
    Guid? OriginalMessageId,
    string? CorrelationId,
    PluginEventSource Source);
