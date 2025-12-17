namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.Inbox;

/// <summary>
/// Repository interface for managing inbox messages
/// </summary>
public interface IInboxRepository
{
    /// <summary>
    /// Adds a new message to the inbox (with duplicate detection)
    /// </summary>
    Task<Result<InboxMessage>> AddAsync(InboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending messages that are ready for processing (not locked or lock expired)
    /// </summary>
    Task<List<InboxMessage>> GetPendingMessagesAsync(int batchSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific message by its ID
    /// </summary>
    Task<InboxMessage?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a message by its unique message ID
    /// </summary>
    Task<InboxMessage?> GetByMessageIdAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a message with the given ID already exists
    /// </summary>
    Task<bool> ExistsAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing message
    /// </summary>
    Task<Result> UpdateAsync(InboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple messages in a single transaction
    /// </summary>
    Task<Result> UpdateBatchAsync(IEnumerable<InboxMessage> messages, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes old processed messages to keep the table clean
    /// </summary>
    Task<int> CleanupOldMessagesAsync(DateTime olderThan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets statistics about the inbox messages
    /// </summary>
    Task<InboxStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases expired locks on messages
    /// </summary>
    Task<int> ReleaseExpiredLocksAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Statistics about inbox messages
/// </summary>
public class InboxStatistics
{
    public int PendingCount { get; set; }
    public int ProcessingCount { get; set; }
    public int ProcessedCount { get; set; }
    public int FailedCount { get; set; }
    public int DuplicateCount { get; set; }
    public DateTime? OldestPendingMessage { get; set; }
    public DateTime? LatestProcessedMessage { get; set; }
}
