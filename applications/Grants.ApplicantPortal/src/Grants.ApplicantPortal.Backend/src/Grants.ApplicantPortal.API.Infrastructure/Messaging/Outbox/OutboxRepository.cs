using Grants.ApplicantPortal.API.Infrastructure.Data;

namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.Outbox;

/// <summary>
/// Repository implementation for managing outbox messages using Entity Framework
/// </summary>
public class OutboxRepository : IOutboxRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger<OutboxRepository> _logger;

    public OutboxRepository(AppDbContext context, ILogger<OutboxRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result> AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.OutboxMessages.Add(message);
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogDebug("Added outbox message {MessageId} of type {MessageType}", 
                message.MessageId, message.MessageType);
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add outbox message {MessageId}", message.MessageId);
            return Result.Error($"Failed to add outbox message: {ex.Message}");
        }
    }

    public async Task<Result> AddBatchAsync(IEnumerable<OutboxMessage> messages, CancellationToken cancellationToken = default)
    {
        var messagesList = messages.ToList();
        if (!messagesList.Any())
        {
            return Result.Success();
        }

        try
        {
            _context.OutboxMessages.AddRange(messagesList);
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogDebug("Added {Count} outbox messages in batch", messagesList.Count);
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add batch of {Count} outbox messages", messagesList.Count);
            return Result.Error($"Failed to add batch of outbox messages: {ex.Message}");
        }
    }

    public async Task<List<OutboxMessage>> GetPendingMessagesAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTime = DateTime.UtcNow;
            
            var messages = await _context.OutboxMessages
                .Where(m => m.Status == OutboxMessageStatus.Pending || 
                           (m.Status == OutboxMessageStatus.Processing && m.LockExpiry < currentTime))
                .OrderBy(m => m.CreatedAt)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            _logger.LogDebug("Retrieved {Count} pending outbox messages", messages.Count);
            
            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get pending outbox messages");
            return new List<OutboxMessage>();
        }
    }

    public async Task<OutboxMessage?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.OutboxMessages
                .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get outbox message by id {Id}", id);
            return null;
        }
    }

    public async Task<OutboxMessage?> GetByMessageIdAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.OutboxMessages
                .FirstOrDefaultAsync(m => m.MessageId == messageId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get outbox message by message id {MessageId}", messageId);
            return null;
        }
    }

    public async Task<Result> UpdateAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.OutboxMessages.Update(message);
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogDebug("Updated outbox message {MessageId}", message.MessageId);
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update outbox message {MessageId}", message.MessageId);
            return Result.Error($"Failed to update outbox message: {ex.Message}");
        }
    }

    public async Task<Result> UpdateBatchAsync(IEnumerable<OutboxMessage> messages, CancellationToken cancellationToken = default)
    {
        var messagesList = messages.ToList();
        if (!messagesList.Any())
        {
            return Result.Success();
        }

        try
        {
            _context.OutboxMessages.UpdateRange(messagesList);
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogDebug("Updated {Count} outbox messages in batch", messagesList.Count);
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update batch of {Count} outbox messages", messagesList.Count);
            return Result.Error($"Failed to update batch of outbox messages: {ex.Message}");
        }
    }

    public async Task<int> CleanupOldMessagesAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        try
        {
            var deletedCount = await _context.OutboxMessages
                .Where(m => (m.Status == OutboxMessageStatus.Published || m.Status == OutboxMessageStatus.Failed) &&
                           m.ProcessedAt < olderThan)
                .ExecuteDeleteAsync(cancellationToken);

            _logger.LogInformation("Cleaned up {Count} old outbox messages older than {OlderThan}", 
                deletedCount, olderThan);
            
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup old outbox messages");
            return 0;
        }
    }

    public async Task<OutboxStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = await _context.OutboxMessages
                .GroupBy(m => 1)
                .Select(g => new OutboxStatistics
                {
                    PendingCount = g.Count(m => m.Status == OutboxMessageStatus.Pending),
                    ProcessingCount = g.Count(m => m.Status == OutboxMessageStatus.Processing),
                    PublishedCount = g.Count(m => m.Status == OutboxMessageStatus.Published),
                    FailedCount = g.Count(m => m.Status == OutboxMessageStatus.Failed),
                    OldestPendingMessage = g.Where(m => m.Status == OutboxMessageStatus.Pending)
                                           .Min(m => (DateTime?)m.CreatedAt),
                    LatestProcessedMessage = g.Where(m => m.Status == OutboxMessageStatus.Published)
                                            .Max(m => (DateTime?)m.ProcessedAt)
                })
                .FirstOrDefaultAsync(cancellationToken);

            return stats ?? new OutboxStatistics();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get outbox statistics");
            return new OutboxStatistics();
        }
    }

    public async Task<int> ReleaseExpiredLocksAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var currentTime = DateTime.UtcNow;
            
            var expiredMessages = await _context.OutboxMessages
                .Where(m => m.Status == OutboxMessageStatus.Processing && m.LockExpiry < currentTime)
                .ToListAsync(cancellationToken);

            foreach (var message in expiredMessages)
            {
                message.ReleaseLock();
            }

            if (expiredMessages.Any())
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Released {Count} expired locks on outbox messages", expiredMessages.Count);
            }

            return expiredMessages.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to release expired locks on outbox messages");
            return 0;
        }
    }
}
