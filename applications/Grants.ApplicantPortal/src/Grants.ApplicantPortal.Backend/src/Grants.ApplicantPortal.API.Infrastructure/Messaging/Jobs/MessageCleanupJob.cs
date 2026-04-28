using Grants.ApplicantPortal.API.Infrastructure.Messaging.BackgroundJobs;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Configuration;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Inbox;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Outbox;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.Jobs;

/// <summary>
/// Hourly cleanup job that removes processed inbox and outbox messages
/// older than the configured retention period (default: 7 days).
/// Only deletes messages in terminal statuses (Published/Failed/TimedOut for outbox,
/// Processed/Failed/Duplicate for inbox) — never touches Pending or Processing.
/// Starts after a configurable startup delay to avoid contention during application boot.
/// </summary>
[DisallowConcurrentExecution]
public class MessageCleanupJob : IJob
{
    private readonly IOutboxRepository _outboxRepository;
    private readonly IInboxRepository _inboxRepository;
    private readonly IDistributedLock _distributedLock;
    private readonly JobCircuitBreaker _circuitBreaker;
    private readonly MessagingOptions _options;
    private readonly ILogger<MessageCleanupJob> _logger;

    private readonly string _lockKey = "message-cleanup";
    private readonly TimeSpan _lockDuration = TimeSpan.FromMinutes(10);

    public MessageCleanupJob(
        IOutboxRepository outboxRepository,
        IInboxRepository inboxRepository,
        IDistributedLock distributedLock,
        JobCircuitBreaker circuitBreaker,
        IOptions<MessagingOptions> options,
        ILogger<MessageCleanupJob> logger)
    {
        _outboxRepository = outboxRepository;
        _inboxRepository = inboxRepository;
        _distributedLock = distributedLock;
        _circuitBreaker = circuitBreaker;
        _options = options.Value;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;

        if (!_circuitBreaker.ShouldExecute(_lockKey))
        {
            return;
        }

        _logger.LogInformation("Message cleanup job starting");

        try
        {
            // Acquire distributed lock to ensure only one instance runs cleanup
            var lockResult = await _distributedLock.AcquireLockAsync(_lockKey, _lockDuration, TimeSpan.FromSeconds(5), cancellationToken);

            if (!lockResult.IsSuccess)
            {
                var lockError = string.Join(", ", lockResult.Errors);
                _logger.LogDebug("Could not acquire lock for message cleanup: {Error}", lockError);

                if (IsInfrastructureLockFailure(lockError))
                {
                    throw new InvalidOperationException($"Distributed lock acquisition failed for {_lockKey}: {lockError}");
                }

                return;
            }

            var lockToken = lockResult.Value;

            try
            {
                var outboxCutoff = DateTime.UtcNow.AddDays(-_options.Outbox.RetentionDays);
                var inboxCutoff = DateTime.UtcNow.AddDays(-_options.Inbox.RetentionDays);

                var outboxDeleted = await _outboxRepository.CleanupOldMessagesAsync(outboxCutoff, cancellationToken);
                var inboxDeleted = await _inboxRepository.CleanupOldMessagesAsync(inboxCutoff, cancellationToken);

                if (outboxDeleted > 0 || inboxDeleted > 0)
                {
                    _logger.LogInformation(
                        "Message cleanup completed. Outbox: {OutboxDeleted} removed (older than {OutboxDays}d), Inbox: {InboxDeleted} removed (older than {InboxDays}d)",
                        outboxDeleted, _options.Outbox.RetentionDays,
                        inboxDeleted, _options.Inbox.RetentionDays);
                }
                else
                {
                    _logger.LogDebug("Message cleanup completed. No messages to remove");
                }
            }
            finally
            {
                var releaseResult = await _distributedLock.ReleaseLockAsync(_lockKey, lockToken, CancellationToken.None);
                if (!releaseResult.IsSuccess)
                {
                    var releaseError = string.Join(", ", releaseResult.Errors);
                    _logger.LogWarning("Failed to release lock for message cleanup: {Error}", releaseError);

                    if (IsInfrastructureLockFailure(releaseError))
                    {
                        throw new InvalidOperationException($"Distributed lock release failed for {_lockKey}: {releaseError}");
                    }
                }
            }

            _circuitBreaker.RecordSuccess(_lockKey);
        }
        catch (Exception ex)
        {
            _circuitBreaker.RecordFailure(_lockKey, ex);
        }
    }

    private static bool IsInfrastructureLockFailure(string? error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            return false;
        }

        var normalized = error.ToLowerInvariant();
        return normalized.Contains("redis") ||
               normalized.Contains("connect") ||
               normalized.Contains("socket") ||
               normalized.Contains("network") ||
               normalized.Contains("error acquiring lock") ||
               normalized.Contains("error releasing lock") ||
               normalized.Contains("error renewing lock");
    }
}
