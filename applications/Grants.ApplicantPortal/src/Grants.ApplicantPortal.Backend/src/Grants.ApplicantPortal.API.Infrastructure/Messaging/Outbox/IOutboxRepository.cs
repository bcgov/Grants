namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.Outbox;

/// <summary>
/// Repository interface for managing outbox messages
/// </summary>
public interface IOutboxRepository
{
    /// <summary>
    /// Adds a new message to the outbox
    /// </summary>
    Task<Result> AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple messages to the outbox as a single transaction
    /// </summary>
    Task<Result> AddBatchAsync(IEnumerable<OutboxMessage> messages, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending messages that are ready for processing (not locked or lock expired)
    /// </summary>
    Task<List<OutboxMessage>> GetPendingMessagesAsync(int batchSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific message by its ID
    /// </summary>
    Task<OutboxMessage?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a message by its unique message ID
    /// </summary>
    Task<OutboxMessage?> GetByMessageIdAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing message
    /// </summary>
    Task<Result> UpdateAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple messages in a single transaction
    /// </summary>
    Task<Result> UpdateBatchAsync(IEnumerable<OutboxMessage> messages, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes old processed messages to keep the table clean
    /// </summary>
    Task<int> CleanupOldMessagesAsync(DateTime olderThan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statistics about the outbox messages
    /// </summary>
    Task<OutboxStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases expired locks on messages
    /// </summary>
    Task<int> ReleaseExpiredLocksAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets messages in Published status that were published before the specified cutoff time.
    /// Used by the ack-timeout job to find messages that never received an acknowledgment.
    /// </summary>
    Task<List<OutboxMessage>> GetPublishedMessagesOlderThanAsync(DateTime cutoff, int batchSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of a message directly from the database, bypassing the
    /// EF change tracker. Used to guard against race conditions where a tracked entity
    /// may have been updated by another scope (e.g. ack processed while timeout job is running).
    /// </summary>
    Task<OutboxMessageStatus?> GetCurrentStatusAsync(long id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Statistics about outbox messages
/// </summary>
public class OutboxStatistics
{
    public int PendingCount { get; set; }
    public int ProcessingCount { get; set; }
    public int PublishedCount { get; set; }
    public int FailedCount { get; set; }
    public int TimedOutCount { get; set; }
    public int AcknowledgedCount { get; set; }
    public DateTime? OldestPendingMessage { get; set; }
    public DateTime? LatestProcessedMessage { get; set; }
}
