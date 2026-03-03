using Grants.ApplicantPortal.API.Infrastructure.Data;

namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.Inbox;

/// <summary>
/// Repository implementation for managing inbox messages using Entity Framework
/// </summary>
public class InboxRepository : IInboxRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<InboxRepository> _logger;

    public InboxRepository(AppDbContext context, ILogger<InboxRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<InboxMessage>> AddAsync(InboxMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check for duplicates
            var existingMessage = await _context.InboxMessages
                .FirstOrDefaultAsync(m => m.MessageId == message.MessageId, cancellationToken);

            if (existingMessage != null)
            {
                _logger.LogDebug("Duplicate message detected: {MessageId}", message.MessageId);
                existingMessage.MarkAsDuplicate();
                await _context.SaveChangesAsync(cancellationToken);
                return Result<InboxMessage>.Success(existingMessage);
            }

            _context.InboxMessages.Add(message);
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogDebug("Added inbox message {MessageId} of type {MessageType}", 
                message.MessageId, message.MessageType);
            
            return Result<InboxMessage>.Success(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add inbox message {MessageId}", message.MessageId);
            return Result<InboxMessage>.Error($"Failed to add inbox message: {ex.Message}");
        }
    }

    public async Task<List<InboxMessage>> GetPendingMessagesAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTime = DateTime.UtcNow;
            
            var messages = await _context.InboxMessages
                .Where(m => m.Status == InboxMessageStatus.Pending || 
                           (m.Status == InboxMessageStatus.Processing && m.LockExpiry < currentTime))
                .OrderBy(m => m.ReceivedAt)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Retrieved {Count} pending inbox messages", messages.Count);
            
            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pending inbox messages");
            return new List<InboxMessage>();
        }
    }

    public async Task<InboxMessage?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.InboxMessages
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get inbox message by id {Id}", id);
            return null;
        }
    }

    public async Task<InboxMessage?> GetByMessageIdAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.InboxMessages
                .FirstOrDefaultAsync(m => m.MessageId == messageId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get inbox message by message id {MessageId}", messageId);
            return null;
        }
    }

    public async Task<bool> ExistsAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.InboxMessages
                .AnyAsync(m => m.MessageId == messageId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if inbox message exists {MessageId}", messageId);
            return false;
        }
    }

    public async Task<Result> UpdateAsync(InboxMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.InboxMessages.Update(message);
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogDebug("Updated inbox message {MessageId}", message.MessageId);
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update inbox message {MessageId}", message.MessageId);
            return Result.Error($"Failed to update inbox message: {ex.Message}");
        }
    }

    public async Task<Result> UpdateBatchAsync(IEnumerable<InboxMessage> messages, CancellationToken cancellationToken = default)
    {
        var messagesList = messages.ToList();
        if (!messagesList.Any())
        {
            return Result.Success();
        }

        try
        {
            _context.InboxMessages.UpdateRange(messagesList);
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogDebug("Updated {Count} inbox messages in batch", messagesList.Count);
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update batch of {Count} inbox messages", messagesList.Count);
            return Result.Error($"Failed to update batch of inbox messages: {ex.Message}");
        }
    }

    public async Task<int> CleanupOldMessagesAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        try
        {
            var deletedCount = await _context.InboxMessages
                .Where(m => (m.Status == InboxMessageStatus.Processed || 
                            m.Status == InboxMessageStatus.Failed ||
                            m.Status == InboxMessageStatus.Duplicate) &&
                           m.ProcessedAt < olderThan)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogInformation("Cleaned up {Count} old inbox messages older than {OlderThan}", 
                deletedCount, olderThan);
            
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old inbox messages");
            return 0;
        }
    }

    public async Task<InboxStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = await _context.InboxMessages
                .GroupBy(m => 1)
                .Select(g => new InboxStatistics
                {
                    PendingCount = g.Count(m => m.Status == InboxMessageStatus.Pending),
                    ProcessingCount = g.Count(m => m.Status == InboxMessageStatus.Processing),
                    ProcessedCount = g.Count(m => m.Status == InboxMessageStatus.Processed),
                    FailedCount = g.Count(m => m.Status == InboxMessageStatus.Failed),
                    DuplicateCount = g.Count(m => m.Status == InboxMessageStatus.Duplicate),
                    OldestPendingMessage = g.Where(m => m.Status == InboxMessageStatus.Pending)
                                           .Min(m => (DateTime?)m.ReceivedAt),
                    LatestProcessedMessage = g.Where(m => m.Status == InboxMessageStatus.Processed)
                                            .Max(m => (DateTime?)m.ProcessedAt)
                })
                .FirstOrDefaultAsync(cancellationToken);

            return stats ?? new InboxStatistics();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get inbox statistics");
            return new InboxStatistics();
        }
    }

    public async Task<int> ReleaseExpiredLocksAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTime = DateTime.UtcNow;
            
            var expiredMessages = await _context.InboxMessages
                .Where(m => m.Status == InboxMessageStatus.Processing && m.LockExpiry < currentTime)
                .ToListAsync(cancellationToken);

            foreach (var message in expiredMessages)
            {
                message.ReleaseLock();
            }

            if (expiredMessages.Any())
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Released {Count} expired locks on inbox messages", expiredMessages.Count);
            }

            return expiredMessages.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to release expired locks on inbox messages");
            return 0;
        }
    }
}
