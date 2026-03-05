using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.Core.Services;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.BackgroundJobs;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Outbox;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.RabbitMQ;
using Quartz;

namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.Jobs;

/// <summary>
/// Background job that processes outbox messages and publishes them to RabbitMQ
/// </summary>
public class OutboxProcessorJob : IJob
{
    private readonly IOutboxRepository _outboxRepository;
    private readonly IDistributedLock _distributedLock;
    private readonly IRabbitMQPublisher? _rabbitMQPublisher; // Nullable for fallback scenarios
    private readonly IPluginEventService _pluginEventService;
    private readonly IPluginCommandMetadataRegistry _metadataRegistry;
    private readonly ILogger<OutboxProcessorJob> _logger;

    // Configuration - these could come from appsettings
    private readonly string _lockKey = "outbox-processor";
    private readonly TimeSpan _lockDuration = TimeSpan.FromMinutes(5);
    private readonly int _batchSize = 100;
    private readonly int _maxRetries = 5;

    public OutboxProcessorJob(
        IOutboxRepository outboxRepository,
        IDistributedLock distributedLock,
        IPluginEventService pluginEventService,
        IPluginCommandMetadataRegistry metadataRegistry,
        ILogger<OutboxProcessorJob> logger,
        IRabbitMQPublisher? rabbitMQPublisher = null)
    {
        _outboxRepository = outboxRepository;
        _distributedLock = distributedLock;
        _pluginEventService = pluginEventService;
        _metadataRegistry = metadataRegistry;
        _rabbitMQPublisher = rabbitMQPublisher;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        
        _logger.LogDebug("Outbox processor job starting");

        // First, release any expired locks
        await _outboxRepository.ReleaseExpiredLocksAsync(cancellationToken);

        // Acquire distributed lock to ensure only one instance processes messages
        var lockResult = await _distributedLock.AcquireLockAsync(_lockKey, _lockDuration, TimeSpan.FromSeconds(5), cancellationToken);
        
        if (!lockResult.IsSuccess)
        {
            _logger.LogDebug("Could not acquire lock for outbox processing: {Error}", string.Join(", ", lockResult.Errors));
            return;
        }

        var lockToken = lockResult.Value;
        
        try
        {
            var processed = 0;
            var startTime = DateTime.UtcNow;
            
            // Process messages in batches
            while (!cancellationToken.IsCancellationRequested)
            {
                var messages = await _outboxRepository.GetPendingMessagesAsync(_batchSize, cancellationToken);
                
                if (!messages.Any())
                {
                    break; // No more messages to process
                }

                foreach (var message in messages)
                {
                    await ProcessMessage(message, cancellationToken);
                    processed++;
                }

                // Renew lock if we're still processing
                if (DateTime.UtcNow.Subtract(startTime) > TimeSpan.FromMinutes(2))
                {
                    await _distributedLock.RenewLockAsync(_lockKey, lockToken, _lockDuration, cancellationToken);
                    startTime = DateTime.UtcNow;
                }
            }

            if (processed > 0)
            {
                _logger.LogInformation("Outbox processor job completed. Processed {Count} messages", processed);
            }
            else
            {
                _logger.LogDebug("Outbox processor job completed. No messages to process");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during outbox processing");
        }
        finally
        {
            // Release the lock
            await _distributedLock.ReleaseLockAsync(_lockKey, lockToken, cancellationToken);
        }
    }

    private async Task ProcessMessage(OutboxMessage message, CancellationToken cancellationToken)
    {
        try
        {
            // Lock the message for processing
            var lockToken = Guid.NewGuid().ToString();
            message.MarkAsProcessing(lockToken, TimeSpan.FromMinutes(5));
            
            var updateResult = await _outboxRepository.UpdateAsync(message, cancellationToken);
            if (!updateResult.IsSuccess)
            {
                _logger.LogError("Failed to lock outbox message {MessageId} for processing", message.MessageId);
                return;
            }

            _logger.LogDebug("Processing outbox message {MessageId} of type {MessageType}", 
                message.MessageId, message.MessageType);

            // Publish to RabbitMQ if available, otherwise simulate
            if (_rabbitMQPublisher != null)
            {
                await PublishMessageToRabbitMQ(message, cancellationToken);
            }
            else
            {
                await SimulateMessagePublishing(message, cancellationToken);
            }

            // Mark as successfully published
            message.MarkAsPublished();
            await _outboxRepository.UpdateAsync(message, cancellationToken);

            _logger.LogDebug("Successfully published outbox message {MessageId}", message.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process outbox message {MessageId}", message.MessageId);

            // Mark as failed
            message.MarkAsFailed(ex.Message, _maxRetries);
            await _outboxRepository.UpdateAsync(message, cancellationToken);

            // If permanently failed (retries exhausted), record a failure event for the user
            if (message.Status == OutboxMessageStatus.Failed)
            {
                await RecordOutboxFailureEventAsync(message, ex.Message, cancellationToken);
            }
        }
    }

    private async Task PublishMessageToRabbitMQ(OutboxMessage message, CancellationToken cancellationToken)
    {
        if (_rabbitMQPublisher == null)
        {
            throw new InvalidOperationException("RabbitMQ publisher is not available");
        }

        // Generate routing key based on message type
        var routingKey = GenerateRoutingKey(message.MessageType, message.PluginId);

        // Publish the message
        var publishResult = await _rabbitMQPublisher.PublishAsync(
            message.MessageType,
            message.Payload,
            routingKey,
            message.CorrelationId,
            cancellationToken);

        if (!publishResult.IsSuccess)
        {
            var errorMessage = $"Failed to publish message to RabbitMQ: {string.Join(", ", publishResult.Errors)}";
            _logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }

        _logger.LogDebug("Published message {MessageId} to RabbitMQ with routing key {RoutingKey}", 
            message.MessageId, routingKey);
    }

    private static string GenerateRoutingKey(string messageType, string? pluginId)
    {
        // Outbound commands use the 'commands.' prefix so they are NOT picked up
        // by the Portal's own inbox consumer (which binds to 'grants.*.#').
        // External systems consume from 'commands.*' and publish acks back to 'grants.*'.
        var prefix = !string.IsNullOrEmpty(pluginId) ? pluginId.ToLowerInvariant() : "system";
        var suffix = messageType.ToLowerInvariant().Replace("message", "");

        return $"commands.{prefix}.{suffix}";
    }

    private async Task SimulateMessagePublishing(OutboxMessage message, CancellationToken cancellationToken)
    {
        // Simulate some processing time
        await Task.Delay(50, cancellationToken);

        _logger.LogWarning("Simulating message publishing for {MessageId} - RabbitMQ not configured", message.MessageId);
    }

    /// <summary>
    /// Parses the outbox message payload via the plugin's metadata provider
    /// and records a PluginEvent for the user.
    /// </summary>
    private async Task RecordOutboxFailureEventAsync(OutboxMessage message, string error, CancellationToken cancellationToken)
    {
        try
        {
            var pluginId = message.PluginId ?? "UNKNOWN";
            var metadata = _metadataRegistry.ParsePayload(pluginId, message.Payload);

            if (metadata == null || metadata.ProfileId == Guid.Empty)
            {
                _logger.LogWarning("Could not extract context from failed outbox message {MessageId}", message.MessageId);
                return;
            }

            var friendlyAction = _metadataRegistry.GetFriendlyActionName(pluginId, metadata.DataType);

            var failureContext = new PluginFailureContext(
                metadata.ProfileId,
                pluginId,
                metadata.Provider,
                metadata.DataType,
                metadata.EntityId,
                $"Your {friendlyAction} could not be sent to the external system. Please try again.",
                $"Outbox publish failed after {message.RetryCount} retries. Last error: {error}",
                message.MessageId,
                message.CorrelationId,
                PluginEventSource.OutboxFailure);

            await _pluginEventService.RecordFailureAsync(failureContext, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record outbox failure event for message {MessageId}", message.MessageId);
        }
    }
}
