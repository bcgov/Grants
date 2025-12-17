namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.Abstractions;

/// <summary>
/// Message context that provides additional information about the message processing environment
/// </summary>
public class MessageContext
{
    public MessageContext(IMessage message, Dictionary<string, object>? properties = null)
    {
        Message = message;
        Properties = properties ?? new Dictionary<string, object>();
        ProcessingStartedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// The message being processed
    /// </summary>
    public IMessage Message { get; }
    
    /// <summary>
    /// Additional properties that can be used by handlers
    /// </summary>
    public Dictionary<string, object> Properties { get; }
    
    /// <summary>
    /// When the processing of this message started
    /// </summary>
    public DateTime ProcessingStartedAt { get; }
    
    /// <summary>
    /// Cancellation token for the processing operation
    /// </summary>
    public CancellationToken CancellationToken { get; set; } = CancellationToken.None;

    /// <summary>
    /// Gets a property value by key
    /// </summary>
    public T? GetProperty<T>(string key)
    {
        if (Properties.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }

    /// <summary>
    /// Sets a property value
    /// </summary>
    public void SetProperty<T>(string key, T value)
    {
        if (value != null)
        {
            Properties[key] = value;
        }
    }
}
