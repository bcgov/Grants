using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.Core.Services;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.BackgroundJobs;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Configuration;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Outbox;
using Microsoft.Extensions.Options;
using Quartz;

namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.Jobs;

/// <summary>
/// Background job that detects outbox messages stuck in Published status
/// with no acknowledgment received within the configured timeout threshold.
/// For each timed-out message the job:
///   1. Records a PluginEvent (Error / AckTimeout) — which also invalidates the cache segment
///   2. Marks the outbox message as TimedOut
///
/// If a late ack arrives after timeout, the Portal still processes it.
/// The cache was already invalidated, so the next read will pull fresh data — no harm done.
/// </summary>
[DisallowConcurrentExecution]
public class OutboxTimeoutJob : IJob
{
    private readonly IOutboxRepository _outboxRepository;
    private readonly IDistributedLock _distributedLock;
    private readonly IPluginEventService _pluginEventService;
    private readonly IPluginCommandMetadataRegistry _metadataRegistry;
    private readonly JobCircuitBreaker _circuitBreaker;
    private readonly MessagingOptions _options;
    private readonly ILogger<OutboxTimeoutJob> _logger;

    private readonly string _lockKey = "outbox-timeout";
    private readonly TimeSpan _lockDuration = TimeSpan.FromMinutes(5);

    public OutboxTimeoutJob(
        IOutboxRepository outboxRepository,
        IDistributedLock distributedLock,
        IPluginEventService pluginEventService,
        IPluginCommandMetadataRegistry metadataRegistry,
        JobCircuitBreaker circuitBreaker,
        IOptions<MessagingOptions> options,
        ILogger<OutboxTimeoutJob> logger)
    {
        _outboxRepository = outboxRepository;
        _distributedLock = distributedLock;
        _pluginEventService = pluginEventService;
        _metadataRegistry = metadataRegistry;
        _circuitBreaker = circuitBreaker;
        _options = options.Value;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;

        if (_options.Outbox.AckTimeoutMinutes <= 0)
        {
            _logger.LogDebug("Ack-timeout processing is disabled (AckTimeoutMinutes = {Minutes})", _options.Outbox.AckTimeoutMinutes);
            return;
        }

        if (!_circuitBreaker.ShouldExecute(_lockKey))
        {
            return;
        }

        _logger.LogDebug("Outbox timeout job starting");

        try
        {
            var lockResult = await _distributedLock.AcquireLockAsync(_lockKey, _lockDuration, TimeSpan.FromSeconds(5), cancellationToken);

            if (!lockResult.IsSuccess)
            {
                var lockError = string.Join(", ", lockResult.Errors);
                _logger.LogDebug("Could not acquire lock for outbox timeout processing: {Error}", lockError);

                if (IsInfrastructureLockFailure(lockError))
                {
                    throw new InvalidOperationException($"Distributed lock acquisition failed for {_lockKey}: {lockError}");
                }

                return;
            }

            var lockToken = lockResult.Value;

            try
            {
                var cutoff = DateTime.UtcNow.AddMinutes(-_options.Outbox.AckTimeoutMinutes);
                var timedOut = 0;

                while (!cancellationToken.IsCancellationRequested)
                {
                    var messages = await _outboxRepository.GetPublishedMessagesOlderThanAsync(
                        cutoff, _options.Outbox.BatchSize, cancellationToken);

                    if (messages.Count == 0)
                    {
                        break;
                    }

                    foreach (var message in messages)
                    {
                        await ProcessTimedOutMessage(message, cancellationToken);
                        timedOut++;
                    }
                }

                if (timedOut > 0)
                {
                    _logger.LogInformation("Outbox timeout job completed. Timed out {Count} messages (threshold: {Minutes}m)",
                        timedOut, _options.Outbox.AckTimeoutMinutes);
                }
                else
                {
                    _logger.LogDebug("Outbox timeout job completed. No timed-out messages found");
                }
            }
            finally
            {
                var releaseResult = await _distributedLock.ReleaseLockAsync(_lockKey, lockToken, cancellationToken);
                if (!releaseResult.IsSuccess)
                {
                    var releaseError = string.Join(", ", releaseResult.Errors);
                    _logger.LogWarning("Failed to release lock for outbox timeout processing: {Error}", releaseError);

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
        return normalized.Contains("timeout") ||
               normalized.Contains("redis") ||
               normalized.Contains("connect") ||
               normalized.Contains("socket") ||
               normalized.Contains("network") ||
               normalized.Contains("error acquiring lock") ||
               normalized.Contains("error releasing lock") ||
               normalized.Contains("error renewing lock");
    }

    private async Task ProcessTimedOutMessage(OutboxMessage message, CancellationToken cancellationToken)
    {
        try
        {
            // Re-check the current DB status to guard against a race with the inbox ack processor.
            // The batch was loaded with tracking; if an ack was processed concurrently the in-memory
            // entity is stale. AsNoTracking in GetCurrentStatusAsync always hits the database.
            var currentStatus = await _outboxRepository.GetCurrentStatusAsync(message.Id, cancellationToken);
            if (currentStatus != OutboxMessageStatus.Published)
            {
                _logger.LogDebug(
                    "Outbox message {MessageId} is no longer Published (current status: {Status}) — skipping timeout",
                    message.MessageId, currentStatus);
                return;
            }

            _logger.LogWarning(
                "Outbox message {MessageId} (type: {MessageType}) published at {PublishedAt} has not been acknowledged — marking as timed out",
                message.MessageId, message.MessageType, message.ProcessedAt);

            // 1. Record a PluginEvent (error severity triggers cache invalidation automatically)
            await RecordTimeoutEventAsync(message, cancellationToken);

            // 2. Mark the outbox message as TimedOut
            message.MarkAsTimedOut();
            await _outboxRepository.UpdateAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process timed-out outbox message {MessageId}", message.MessageId);
        }
    }

    /// <summary>
    /// Parses the outbox message payload via the plugin's metadata provider
    /// and records a PluginEvent so the user is notified and the cache is invalidated.
    /// </summary>
    private async Task RecordTimeoutEventAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        try
        {
            var pluginId = message.PluginId ?? "UNKNOWN";
            var metadata = _metadataRegistry.ParsePayload(pluginId, message.Payload);

            if (metadata == null || metadata.ProfileId == Guid.Empty)
            {
                _logger.LogWarning("Could not extract context from timed-out outbox message {MessageId}", message.MessageId);
                return;
            }

            var friendlyAction = _metadataRegistry.GetFriendlyActionName(pluginId, metadata.DataType);

            var timeoutContext = new PluginEventContext(
                metadata.ProfileId,
                pluginId,
                metadata.Provider,
                metadata.DataType,
                metadata.EntityId,
                PluginEventSeverity.Error,
                PluginEventSource.AckTimeout,
                $"Your {friendlyAction} was sent but the external system did not respond in time. The data shown may be outdated — please verify or try again.",
                $"Published at {message.ProcessedAt:O}, timed out after {_options.Outbox.AckTimeoutMinutes} minute(s). MessageType: {message.MessageType}",
                message.MessageId,
                message.CorrelationId);

            await _pluginEventService.RecordFailureAsync(timeoutContext, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record timeout event for outbox message {MessageId}", message.MessageId);
        }
    }
}
