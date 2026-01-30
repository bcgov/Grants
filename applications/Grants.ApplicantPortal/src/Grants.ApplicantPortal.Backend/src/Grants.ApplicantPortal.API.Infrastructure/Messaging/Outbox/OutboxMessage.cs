namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.Outbox;

/// <summary>
/// Represents the status of an outbox message
/// </summary>
public enum OutboxMessageStatus
{
    /// <summary>
    /// Message is pending publication
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Message has been successfully published
    /// </summary>
    Published = 1,
    
    /// <summary>
    /// Message publication failed after maximum retries
    /// </summary>
    Failed = 2,
    
    /// <summary>
    /// Message processing is in progress (locked)
    /// </summary>
    Processing = 3
}

/// <summary>
/// Represents a message in the outbox pattern
/// </summary>
public class OutboxMessage : HasDomainEventsBase
{
    // Private constructor for EF Core
    private OutboxMessage() { }

    public OutboxMessage(
        Guid messageId,
        string messageType, 
        string payload,
        string? pluginId = null,
        string? correlationId = null)
    {
        MessageId = messageId;
        MessageType = Guard.Against.NullOrWhiteSpace(messageType);
        Payload = Guard.Against.NullOrWhiteSpace(payload);
        PluginId = pluginId;
        CorrelationId = correlationId;
        CreatedAt = DateTime.UtcNow;
        Status = OutboxMessageStatus.Pending;
        RetryCount = 0;
    }

    /// <summary>
    /// Primary key
    /// </summary>
    public long Id { get; private set; }

    /// <summary>
    /// Unique identifier for the message (should match the original message ID)
    /// </summary>
    public Guid MessageId { get; private set; }

    /// <summary>
    /// Type of the message (used for routing and deserialization)
    /// </summary>
    public string MessageType { get; private set; } = null!;

    /// <summary>
    /// Serialized message payload
    /// </summary>
    public string Payload { get; private set; } = null!;

    /// <summary>
    /// When the message was created
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the message was processed (published)
    /// </summary>
    public DateTime? ProcessedAt { get; private set; }

    /// <summary>
    /// Current status of the message
    /// </summary>
    public OutboxMessageStatus Status { get; private set; }

    /// <summary>
    /// Number of times processing has been attempted
    /// </summary>
    public int RetryCount { get; private set; }

    /// <summary>
    /// Last error message if processing failed
    /// </summary>
    public string? LastError { get; private set; }

    /// <summary>
    /// ID of the plugin that created this message
    /// </summary>
    public string? PluginId { get; private set; }

    /// <summary>
    /// Correlation ID for tracing related messages
    /// </summary>
    public string? CorrelationId { get; private set; }

    /// <summary>
    /// Lock token for distributed processing
    /// </summary>
    public string? LockToken { get; private set; }

    /// <summary>
    /// When the lock expires
    /// </summary>
    public DateTime? LockExpiry { get; private set; }

    /// <summary>
    /// Marks the message as being processed with a lock
    /// </summary>
    public void MarkAsProcessing(string lockToken, TimeSpan lockDuration)
    {
        Guard.Against.NullOrWhiteSpace(lockToken);
        
        Status = OutboxMessageStatus.Processing;
        LockToken = lockToken;
        LockExpiry = DateTime.UtcNow.Add(lockDuration);
    }

    /// <summary>
    /// Marks the message as successfully published
    /// </summary>
    public void MarkAsPublished()
    {
        Status = OutboxMessageStatus.Published;
        ProcessedAt = DateTime.UtcNow;
        LockToken = null;
        LockExpiry = null;
    }

    /// <summary>
    /// Marks the message as failed and increments retry count
    /// </summary>
    public void MarkAsFailed(string error, int maxRetries)
    {
        Guard.Against.NullOrWhiteSpace(error);
        
        RetryCount++;
        LastError = error;
        
        if (RetryCount >= maxRetries)
        {
            Status = OutboxMessageStatus.Failed;
            ProcessedAt = DateTime.UtcNow;
        }
        else
        {
            Status = OutboxMessageStatus.Pending;
        }
        
        LockToken = null;
        LockExpiry = null;
    }

    /// <summary>
    /// Releases the lock on the message
    /// </summary>
    public void ReleaseLock()
    {
        if (Status == OutboxMessageStatus.Processing)
        {
            Status = OutboxMessageStatus.Pending;
        }
        LockToken = null;
        LockExpiry = null;
    }

    /// <summary>
    /// Checks if the message lock has expired
    /// </summary>
    public bool IsLockExpired()
    {
        return LockExpiry.HasValue && LockExpiry.Value < DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the message can be processed (not locked or lock expired)
    /// </summary>
    public bool CanBeProcessed()
    {
        return Status == OutboxMessageStatus.Pending || IsLockExpired();
    }
}
