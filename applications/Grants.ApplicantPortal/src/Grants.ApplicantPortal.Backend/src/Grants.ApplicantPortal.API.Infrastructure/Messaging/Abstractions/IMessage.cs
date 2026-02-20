namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.Abstractions;

/// <summary>
/// Base interface for all messages in the system
/// </summary>
public interface IMessage
{
    /// <summary>
    /// Unique identifier for this message
    /// </summary>
    Guid MessageId { get; }
    
    /// <summary>
    /// Type of the message (used for routing and deserialization)
    /// </summary>
    string MessageType { get; }
    
    /// <summary>
    /// When the message was created
    /// </summary>
    DateTime CreatedAt { get; }
    
    /// <summary>
    /// Optional correlation ID for tracing related messages
    /// </summary>
    string? CorrelationId { get; }
    
    /// <summary>
    /// ID of the plugin that created this message (if applicable)
    /// </summary>
    string? PluginId { get; }
}

/// <summary>
/// Base implementation for messages
/// </summary>
public abstract record BaseMessage : IMessage
{
    protected BaseMessage()
    {
        MessageId = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        MessageType = GetType().Name;
    }
    
    protected BaseMessage(string? correlationId, string? pluginId) : this()
    {
        CorrelationId = correlationId;
        PluginId = pluginId;
    }

    public Guid MessageId { get; init; }
    public string MessageType { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? CorrelationId { get; init; }
    public string? PluginId { get; init; }
}
