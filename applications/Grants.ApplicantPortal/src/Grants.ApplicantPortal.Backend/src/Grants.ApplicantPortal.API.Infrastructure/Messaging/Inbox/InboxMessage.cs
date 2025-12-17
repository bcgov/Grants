namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.Inbox;

/// <summary>
/// Represents the status of an inbox message
/// </summary>
public enum InboxMessageStatus
{
    /// <summary>
    /// Message is pending processing
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Message has been successfully processed
    /// </summary>
    Processed = 1,
    
    /// <summary>
    /// Message processing failed after maximum retries
    /// </summary>
    Failed = 2,
    
    /// <summary>
    /// Message processing is in progress (locked)
    /// </summary>
    Processing = 3,
    
    /// <summary>
    /// Message is a duplicate and was ignored
    /// </summary>
    Duplicate = 4
}

/// <summary>
/// Represents a message in the inbox pattern
/// </summary>
public class InboxMessage : HasDomainEventsBase
{
    // Private constructor for EF Core
    private InboxMessage() { }

    public InboxMessage(
        Guid messageId,
        string messageType,
        string payload,
        string? correlationId = null)
    {
        MessageId = messageId;
        MessageType = Guard.Against.NullOrWhiteSpace(messageType);
        Payload = Guard.Against.NullOrWhiteSpace(payload);
        CorrelationId = correlationId;
        ReceivedAt = DateTime.UtcNow;
        Status = InboxMessageStatus.Pending;
        RetryCount = 0;
    }

    /// <summary>
    /// Primary key
    /// </summary>
    public long Id { get; private set; }

    /// <summary>
    /// Unique identifier for the message
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
    /// When the message was received
    /// </summary>
    public DateTime ReceivedAt { get; private set; }

    /// <summary>
    /// When the message was processed
    /// </summary>
    public DateTime? ProcessedAt { get; private set; }

    /// <summary>
    /// Current status of the message
    /// </summary>
    public InboxMessageStatus Status { get; private set; }

    /// <summary>
    /// Number of times processing has been attempted
    /// </summary>
    public int RetryCount { get; private set; }

    /// <summary>
    /// Last error message if processing failed
    /// </summary>
    public string? LastError { get; private set; }

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
        
        Status = InboxMessageStatus.Processing;
        LockToken = lockToken;
        LockExpiry = DateTime.UtcNow.Add(lockDuration);
    }

    /// <summary>
    /// Marks the message as successfully processed
    /// </summary>
    public void MarkAsProcessed()
    {
        Status = InboxMessageStatus.Processed;
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
            Status = InboxMessageStatus.Failed;
            ProcessedAt = DateTime.UtcNow;
        }
        else
        {
            Status = InboxMessageStatus.Pending;
        }
        
        LockToken = null;
        LockExpiry = null;
    }

    /// <summary>
    /// Marks the message as a duplicate
    /// </summary>
    public void MarkAsDuplicate()
    {
        Status = InboxMessageStatus.Duplicate;
        ProcessedAt = DateTime.UtcNow;
        LockToken = null;
        LockExpiry = null;
    }

    /// <summary>
    /// Releases the lock on the message
    /// </summary>
    public void ReleaseLock()
    {
        if (Status == InboxMessageStatus.Processing)
        {
            Status = InboxMessageStatus.Pending;
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
        return Status == InboxMessageStatus.Pending || IsLockExpired();
    }
}
