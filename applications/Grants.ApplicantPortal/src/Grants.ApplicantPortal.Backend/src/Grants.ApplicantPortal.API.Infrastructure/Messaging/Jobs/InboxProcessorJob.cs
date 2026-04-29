using System.Text.Json;
using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.Core.Services;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Abstractions;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.BackgroundJobs;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Inbox;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Messages;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Outbox;
using Quartz;

namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.Jobs;

/// <summary>
/// Background job that processes inbox messages and routes them to appropriate handlers
/// </summary>
[DisallowConcurrentExecution]
public class InboxProcessorJob : IJob
{
    private readonly IInboxRepository _inboxRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IDistributedLock _distributedLock;
    private readonly IMessageHandlerResolver? _messageHandlerResolver;
    private readonly IPluginMessageRouter? _pluginMessageRouter;
    private readonly IPluginEventService _pluginEventService;
    private readonly IPluginCommandMetadataRegistry _metadataRegistry;
    private readonly JobCircuitBreaker _circuitBreaker;
    private readonly ILogger<InboxProcessorJob> _logger;

    // Configuration - these could come from appsettings
    private readonly string _lockKey = "inbox-processor";
    private readonly TimeSpan _lockDuration = TimeSpan.FromMinutes(5);
    private readonly int _batchSize = 50;
    private readonly int _maxRetries = 3;
    private readonly JsonSerializerOptions _jsonOptions;

    public InboxProcessorJob(
        IInboxRepository inboxRepository,
        IOutboxRepository outboxRepository,
        IDistributedLock distributedLock,
        IPluginEventService pluginEventService,
        IPluginCommandMetadataRegistry metadataRegistry,
        JobCircuitBreaker circuitBreaker,
        ILogger<InboxProcessorJob> logger,
        IMessageHandlerResolver? messageHandlerResolver = null,
        IPluginMessageRouter? pluginMessageRouter = null)
    {
        _inboxRepository = inboxRepository;
        _outboxRepository = outboxRepository;
        _distributedLock = distributedLock;
        _pluginEventService = pluginEventService;
        _metadataRegistry = metadataRegistry;
        _circuitBreaker = circuitBreaker;
        _messageHandlerResolver = messageHandlerResolver;
        _pluginMessageRouter = pluginMessageRouter;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;

        if (!_circuitBreaker.ShouldExecute(_lockKey))
        {
            return;
        }

        _logger.LogDebug("Inbox processor job starting");

        try
        {
            // Acquire distributed lock to ensure only one instance processes messages
            var lockResult = await _distributedLock.AcquireLockAsync(_lockKey, _lockDuration, TimeSpan.FromSeconds(5), cancellationToken);

            if (!lockResult.IsSuccess)
            {
                var lockError = string.Join(", ", lockResult.Errors);
                _logger.LogDebug("Could not acquire lock for inbox processing: {Error}", lockError);

                if (IsInfrastructureLockFailure(lockError))
                {
                    throw new DistributedLockException($"Distributed lock acquisition failed for {_lockKey}: {lockError}");
                }

                return;
            }

            var lockToken = lockResult.Value;

            try
            {
                // Release any expired application-level inbox message locks (only the pod that won the distributed lock does this)
                await _inboxRepository.ReleaseExpiredLocksAsync(cancellationToken);

                var processed = 0;
                var startTime = DateTime.UtcNow;

                // Process messages in batches
                while (!cancellationToken.IsCancellationRequested)
                {
                    var messages = await _inboxRepository.GetPendingMessagesAsync(_batchSize, cancellationToken);

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
                        var renewResult = await _distributedLock.RenewLockAsync(_lockKey, lockToken, _lockDuration, cancellationToken);
                        if (!renewResult.IsSuccess)
                        {
                            var renewError = string.Join(", ", renewResult.Errors);
                            _logger.LogWarning("Failed to renew lock for inbox processing: {Error}", renewError);

                            if (IsInfrastructureLockFailure(renewError))
                            {
                                throw new DistributedLockException($"Distributed lock renewal failed for {_lockKey}: {renewError}");
                            }
                        }

                        startTime = DateTime.UtcNow;
                    }
                }

                if (processed > 0)
                {
                    _logger.LogInformation("Inbox processor job completed. Processed {Count} messages", processed);
                }
                else
                {
                    _logger.LogDebug("Inbox processor job completed. No messages to process");
                }
            }
            finally
            {
                var releaseResult = await _distributedLock.ReleaseLockAsync(_lockKey, lockToken, CancellationToken.None);
                if (!releaseResult.IsSuccess)
                {
                    var releaseError = string.Join(", ", releaseResult.Errors);
                    _logger.LogWarning("Failed to release lock for inbox processing: {Error}", releaseError);

                    if (IsInfrastructureLockFailure(releaseError))
                    {
                        throw new DistributedLockException($"Distributed lock release failed for {_lockKey}: {releaseError}");
                    }
                }
            }

            _circuitBreaker.RecordSuccess(_lockKey);
        }
        catch (DistributedLockException ex)
        {
            _circuitBreaker.RecordFailure(_lockKey, ex);
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

    private async Task ProcessMessage(InboxMessage message, CancellationToken cancellationToken)
    {
        try
        {
            // Lock the message for processing
            var lockToken = Guid.NewGuid().ToString();
            message.MarkAsProcessing(lockToken, TimeSpan.FromMinutes(5));
            
            var updateResult = await _inboxRepository.UpdateAsync(message, cancellationToken);
            if (!updateResult.IsSuccess)
            {
                _logger.LogError("Failed to lock inbox message {MessageId} for processing", message.MessageId);
                return;
            }

            _logger.LogDebug("Processing inbox message {MessageId} of type {MessageType}", 
                message.MessageId, message.MessageType);

            // Process the message based on available handlers
            if (IsPluginSpecificMessage(message))
            {
                await ProcessWithPluginRouter(message, cancellationToken);
            }
            else if (_messageHandlerResolver != null)
            {
                await ProcessWithHandlers(message, cancellationToken);
            }
            else
            {
                await SimulateMessageProcessing(message, cancellationToken);
            }

            // Mark as successfully processed
            message.MarkAsProcessed();
            await _inboxRepository.UpdateAsync(message, cancellationToken);

            // Close the outbox loop: if this was an acknowledgment, mark the original outbox message
            await TryAcknowledgeOutboxMessageAsync(message, cancellationToken);

            _logger.LogDebug("Successfully processed inbox message {MessageId}", message.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process inbox message {MessageId}", message.MessageId);
            
            // Mark as failed
            message.MarkAsFailed(ex.Message, _maxRetries);
            await _inboxRepository.UpdateAsync(message, cancellationToken);
        }
    }

    private async Task ProcessWithHandlers(InboxMessage message, CancellationToken cancellationToken)
    {
        if (_messageHandlerResolver == null)
        {
            throw new InvalidOperationException("Message handler resolver is not available");
        }

        try
        {
            // Deserialize the message based on its type
            var messageObject = DeserializeMessage(message);
            if (messageObject == null)
            {
                throw new InvalidOperationException($"Could not deserialize message of type {message.MessageType}");
            }

            // Get handlers for this message type
            var handlers = _messageHandlerResolver.GetHandlers(messageObject).ToList();
            
            if (!handlers.Any())
            {
                _logger.LogWarning("No handlers found for message type {MessageType}", message.MessageType);
                return;
            }

            // Create message context
            var context = new MessageContext(messageObject)
            {
                CancellationToken = cancellationToken
            };
            context.SetProperty("InboxMessageId", message.Id);
            context.SetProperty("ReceivedAt", message.ReceivedAt);

            // Execute all handlers
            foreach (var handler in handlers)
            {
                await ExecuteHandler(handler, messageObject, context);
            }

            _logger.LogDebug("Processed message {MessageId} with {HandlerCount} handlers", 
                message.MessageId, handlers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message {MessageId} with handlers", message.MessageId);
            throw;
        }
    }

    private async Task ExecuteHandler(object handler, IMessage message, MessageContext context)
    {
        try
        {
            var handlerType = handler.GetType();
            var messageType = message.GetType();
            
            // Find the HandleAsync method
            var method = handlerType.GetMethod("HandleAsync", new[] { messageType, typeof(MessageContext) });
            if (method == null)
            {
                _logger.LogWarning("Handler {HandlerType} does not have HandleAsync method for {MessageType}", 
                    handlerType.Name, messageType.Name);
                return;
            }

            // Invoke the handler
            var task = (Task<Result>?)method.Invoke(handler, new object[] { message, context });
            if (task != null)
            {
                var result = await task;
                if (!result.IsSuccess)
                {
                    _logger.LogError("Handler {HandlerType} failed for message {MessageId}: {Errors}", 
                        handlerType.Name, message.MessageId, string.Join(", ", result.Errors));
                    throw new InvalidOperationException($"Handler {handlerType.Name} failed: {string.Join(", ", result.Errors)}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing handler {HandlerType} for message {MessageId}", 
                handler.GetType().Name, message.MessageId);
            throw;
        }
    }

    private IMessage? DeserializeMessage(InboxMessage inboxMessage)
    {
        try
        {
            // Map message types to their corresponding classes
            var messageType = inboxMessage.MessageType switch
            {
                "ProfileUpdatedMessage" => typeof(Messages.ProfileUpdatedMessage),
                "ContactCreatedMessage" => typeof(Messages.ContactCreatedMessage),
                "AddressUpdatedMessage" => typeof(Messages.AddressUpdatedMessage),
                "OrganizationUpdatedMessage" => typeof(Messages.OrganizationUpdatedMessage),
                "SystemEventMessage" => typeof(Messages.SystemEventMessage),
                "MessageAcknowledgment" => typeof(Messages.MessageAcknowledgment),
                "PluginDataMessage" => typeof(Messages.PluginDataMessage),
                _ => null
            };

            if (messageType == null)
            {
                _logger.LogWarning("Unknown message type: {MessageType}", inboxMessage.MessageType);
                return null;
            }

            var messageObject = JsonSerializer.Deserialize(inboxMessage.Payload, messageType, _jsonOptions);
            return messageObject as IMessage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize message {MessageId} of type {MessageType}", 
                inboxMessage.MessageId, inboxMessage.MessageType);
            return null;
        }
    }

    private bool IsPluginSpecificMessage(InboxMessage message)
    {
        // Check if this is a plugin-specific message (acknowledgment, plugin data, etc.)
        var pluginMessageTypes = new[] { "MessageAcknowledgment", "PluginDataMessage" };
        return pluginMessageTypes.Contains(message.MessageType);
    }

    private async Task ProcessWithPluginRouter(InboxMessage message, CancellationToken cancellationToken)
    {
        if (_pluginMessageRouter == null)
        {
            throw new InvalidOperationException("Plugin message router is not available");
        }

        try
        {
            // Deserialize the message based on its type
            var messageObject = DeserializeMessage(message);
            if (messageObject == null)
            {
                throw new InvalidOperationException($"Could not deserialize plugin message of type {message.MessageType}");
            }

            // Create message context
            var context = new MessageContext(messageObject)
            {
                CancellationToken = cancellationToken
            };
            context.SetProperty("InboxMessageId", message.Id);
            context.SetProperty("ReceivedAt", message.ReceivedAt);

            // Route to plugin handler
            var result = await _pluginMessageRouter.RouteToPluginAsync(messageObject, context);
            
            if (!result.IsSuccess)
            {
                var errorMessage = $"Plugin router failed: {string.Join(", ", result.Errors)}";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            _logger.LogDebug("Successfully processed plugin message {MessageId} of type {MessageType}", 
                message.MessageId, message.MessageType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing plugin message {MessageId} with router", message.MessageId);
            throw;
        }
    }

    private async Task SimulateMessageProcessing(InboxMessage message, CancellationToken cancellationToken)
    {
        // Simulate some processing time
        await Task.Delay(100, cancellationToken);

        _logger.LogWarning("Simulating message processing for {MessageId} - message handlers not configured", 
            message.MessageId);
    }

    /// <summary>
    /// If the inbox message is a <see cref="MessageAcknowledgment"/>, looks up the original
    /// outbox message by <c>OriginalMessageId</c> and marks it as <see cref="OutboxMessageStatus.Acknowledged"/>.
    /// This closes the outbox→ack loop so the timeout job won't flag acknowledged messages.
    /// Works for both normal acks (Published → Acknowledged) and late acks (TimedOut → Acknowledged).
    /// For FAILED acks, also records a PluginEvent (which triggers cache invalidation) so the
    /// user is notified and stale optimistic data is evicted.
    /// </summary>
    private async Task TryAcknowledgeOutboxMessageAsync(InboxMessage inboxMessage, CancellationToken cancellationToken)
    {
        if (inboxMessage.MessageType != nameof(MessageAcknowledgment))
        {
            return;
        }

        try
        {
            var ack = JsonSerializer.Deserialize<MessageAcknowledgment>(inboxMessage.Payload, _jsonOptions);
            if (ack == null || ack.OriginalMessageId == Guid.Empty)
            {
                return;
            }

            var outboxMessage = await _outboxRepository.GetByMessageIdAsync(ack.OriginalMessageId, cancellationToken);
            if (outboxMessage == null)
            {
                _logger.LogDebug("No outbox message found for ack OriginalMessageId {OriginalMessageId} — may have been cleaned up",
                    ack.OriginalMessageId);
                return;
            }

            // Record a PluginEvent for FAILED acks so the user is notified and cache is invalidated
            if (ack.Status.Equals("FAILED", StringComparison.OrdinalIgnoreCase))
            {
                await RecordAckFailureEventAsync(outboxMessage, ack, cancellationToken);
            }

            if (outboxMessage.Status == OutboxMessageStatus.Acknowledged)
            {
                _logger.LogDebug("Outbox message {MessageId} already acknowledged — skipping duplicate ack",
                    outboxMessage.MessageId);
                return;
            }

            var previousStatus = outboxMessage.Status;
            outboxMessage.MarkAsAcknowledged();
            await _outboxRepository.UpdateAsync(outboxMessage, cancellationToken);

            _logger.LogInformation(
                "Closed outbox loop: message {MessageId} transitioned {PreviousStatus} → Acknowledged (ack status: {AckStatus})",
                outboxMessage.MessageId, previousStatus, ack.Status);
        }
        catch (Exception ex)
        {
            // Don't let outbox bookkeeping failures break inbox processing
            _logger.LogWarning(ex, "Failed to acknowledge outbox message for inbox message {MessageId} — non-fatal",
                inboxMessage.MessageId);
        }
    }

    /// <summary>
    /// Parses the original outbox message payload via the plugin's metadata provider
    /// and records a PluginEvent so the user is notified and the cache is invalidated.
    /// </summary>
    private async Task RecordAckFailureEventAsync(OutboxMessage outboxMessage, MessageAcknowledgment ack, CancellationToken cancellationToken)
    {
        try
        {
            var pluginId = outboxMessage.PluginId ?? ack.PluginId ?? "UNKNOWN";
            var metadata = _metadataRegistry.ParsePayload(pluginId, outboxMessage.Payload);

            if (metadata == null || metadata.ProfileId == Guid.Empty)
            {
                _logger.LogWarning("Could not extract context from outbox message {MessageId} for failed ack",
                    outboxMessage.MessageId);
                return;
            }

            var friendlyAction = _metadataRegistry.GetFriendlyActionName(pluginId, metadata.DataType);

            var failureContext = new PluginEventContext(
                metadata.ProfileId,
                pluginId,
                metadata.Provider,
                metadata.DataType,
                metadata.EntityId,
                PluginEventSeverity.Error,
                PluginEventSource.InboxRejection,
                $"Your {friendlyAction} was rejected by the external system: {ack.Details ?? "no details provided"}. Please try again or contact support.",
                $"FAILED ack received for MessageId: {outboxMessage.MessageId}, Details: {ack.Details}",
                outboxMessage.MessageId,
                outboxMessage.CorrelationId);

            await _pluginEventService.RecordFailureAsync(failureContext, cancellationToken);

            _logger.LogWarning(
                "Recorded failure event for FAILED ack on outbox message {MessageId} (plugin: {PluginId}, dataType: {DataType})",
                outboxMessage.MessageId, pluginId, metadata.DataType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record ack failure event for outbox message {MessageId}",
                outboxMessage.MessageId);
        }
    }
}
