namespace Grants.ApplicantPortal.API.Core.Plugins;

/// <summary>
/// Context for recording any plugin event (info, warning, or error).
/// For errors, the event system will also trigger compensation (e.g. cache invalidation).
/// </summary>
public record PluginEventContext(
    Guid ProfileId,
    string PluginId,
    string Provider,
    string DataType,
    string? EntityId,
    PluginEventSeverity Severity,
    PluginEventSource Source,
    string UserMessage,
    string? TechnicalDetails = null,
    Guid? OriginalMessageId = null,
    string? CorrelationId = null);
