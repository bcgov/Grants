using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.Core.Services;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Abstractions;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Messages;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Outbox;

namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.Handlers;

/// <summary>
/// Unity plugin message handler for processing acknowledgments and incoming data messages
/// from the Unity external system via the inbox pattern.
/// </summary>
public class UnityPluginMessageHandler : IPluginMessageHandler
{
    private readonly IPluginEventService _pluginEventService;
    private readonly IPluginCommandMetadataRegistry _metadataRegistry;
    private readonly IOutboxRepository _outboxRepository;
    private readonly ILogger<UnityPluginMessageHandler> _logger;

    public UnityPluginMessageHandler(
        IPluginEventService pluginEventService,
        IPluginCommandMetadataRegistry metadataRegistry,
        IOutboxRepository outboxRepository,
        ILogger<UnityPluginMessageHandler> logger)
    {
        _pluginEventService = pluginEventService;
        _metadataRegistry = metadataRegistry;
        _outboxRepository = outboxRepository;
        _logger = logger;
    }

    public string PluginId => "UNITY";

    public async Task<Result> HandleAcknowledgmentAsync(MessageAcknowledgment acknowledgment, MessageContext context)
    {
        try
        {
            _logger.LogInformation(
                "Unity plugin received acknowledgment for message {OriginalMessageId} with status {Status}",
                acknowledgment.OriginalMessageId, acknowledgment.Status);

            switch (acknowledgment.Status.ToUpperInvariant())
            {
                case "SUCCESS":
                    await HandleSuccessfulAcknowledgment(acknowledgment, context);
                    break;
                case "FAILED":
                    await HandleFailedAcknowledgment(acknowledgment, context);
                    break;
                case "PROCESSING":
                    await HandleProcessingAcknowledgment(acknowledgment, context);
                    break;
                default:
                    _logger.LogWarning(
                        "Unknown acknowledgment status {Status} for message {OriginalMessageId}",
                        acknowledgment.Status, acknowledgment.OriginalMessageId);
                    break;
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error handling acknowledgment for message {OriginalMessageId}",
                acknowledgment.OriginalMessageId);
            return Result.Error($"Failed to handle acknowledgment: {ex.Message}");
        }
    }

    public async Task<Result> HandleIncomingDataAsync(PluginDataMessage message, MessageContext context)
    {
        try
        {
            _logger.LogInformation("Unity plugin received data message of type {DataType}", message.DataType);

            switch (message.DataType.ToUpperInvariant())
            {
                case "PROFILE_UPDATE":
                    await HandleProfileUpdateData(message, context);
                    break;
                case "CONTACT_SYNC":
                    await HandleContactSyncData(message, context);
                    break;
                case "ADDRESS_SYNC":
                    await HandleAddressSyncData(message, context);
                    break;
                case "STATUS_UPDATE":
                    await HandleStatusUpdateData(message, context);
                    break;
                default:
                    _logger.LogInformation(
                        "Unity plugin received data type {DataType}, logging for reference",
                        message.DataType);
                    _logger.LogDebug("Data message payload: {@Data}", message.Data);
                    break;
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling incoming data message {MessageId}", message.MessageId);
            return Result.Error($"Failed to handle incoming data: {ex.Message}");
        }
    }

    public bool CanHandle(IMessage message)
    {
        return message.PluginId != null &&
               message.PluginId.Equals(PluginId, StringComparison.OrdinalIgnoreCase);
    }

    private Task HandleSuccessfulAcknowledgment(MessageAcknowledgment acknowledgment, MessageContext context)
    {
        _logger.LogInformation(
            "Unity: External system successfully processed message {OriginalMessageId}. Details: {Details}",
            acknowledgment.OriginalMessageId, acknowledgment.Details);

        // TODO: Update internal state — mark the operation as completed, update cache, etc.
        return Task.CompletedTask;
    }

    private async Task HandleFailedAcknowledgment(MessageAcknowledgment acknowledgment, MessageContext context)
    {
        _logger.LogWarning(
            "Unity: External system failed to process message {OriginalMessageId}. Details: {Details}",
            acknowledgment.OriginalMessageId, acknowledgment.Details);

        // Look up the original outbox message and parse via the registry
        var originalMessage = await _outboxRepository.GetByMessageIdAsync(
            acknowledgment.OriginalMessageId, context.CancellationToken);

        CommandPayloadMetadata? metadata = null;
        if (originalMessage != null)
        {
            metadata = _metadataRegistry.ParsePayload(PluginId, originalMessage.Payload);
        }

        var profileId = metadata?.ProfileId ?? Guid.Empty;

        // Fallback: extract profileId from CorrelationId if not found in payload
        if (profileId == Guid.Empty && acknowledgment.CorrelationId != null &&
            acknowledgment.CorrelationId.StartsWith("profile-", StringComparison.OrdinalIgnoreCase))
        {
            Guid.TryParse(acknowledgment.CorrelationId["profile-".Length..], out profileId);
        }

        if (profileId == Guid.Empty)
        {
            _logger.LogWarning(
                "Could not determine profileId for failed ack {OriginalMessageId} — skipping event recording",
                acknowledgment.OriginalMessageId);
            return;
        }

        var dataType = metadata?.DataType ?? "UNKNOWN";
        var friendlyAction = _metadataRegistry.GetFriendlyActionName(PluginId, dataType);

        var failureContext = new PluginFailureContext(
            profileId,
            PluginId,
            metadata?.Provider ?? "UNKNOWN",
            dataType,
            metadata?.EntityId,
            $"The external system rejected your {friendlyAction}: {acknowledgment.Details ?? "no details provided"}. Your data may revert on next refresh.",
            $"External system FAILED ack for message {acknowledgment.OriginalMessageId}. Details: {acknowledgment.Details}",
            acknowledgment.OriginalMessageId,
            acknowledgment.CorrelationId,
            PluginEventSource.InboxRejection);

        await _pluginEventService.RecordFailureAsync(failureContext, context.CancellationToken);
    }

    private Task HandleProcessingAcknowledgment(MessageAcknowledgment acknowledgment, MessageContext context)
    {
        _logger.LogInformation(
            "Unity: External system is still processing message {OriginalMessageId}. Details: {Details}",
            acknowledgment.OriginalMessageId, acknowledgment.Details);

        // TODO: Update status indicators, extend timeouts, etc.
        return Task.CompletedTask;
    }

    private Task HandleProfileUpdateData(PluginDataMessage message, MessageContext context)
    {
        _logger.LogInformation("Unity: Processing profile update data from external system");
        // TODO: Process the profile update — update local profile data, trigger UI refresh, etc.
        return Task.CompletedTask;
    }

    private Task HandleContactSyncData(PluginDataMessage message, MessageContext context)
    {
        _logger.LogInformation("Unity: Processing contact sync data from external system");
        // TODO: Synchronize contact data — update contact information, merge duplicates, etc.
        return Task.CompletedTask;
    }

    private Task HandleAddressSyncData(PluginDataMessage message, MessageContext context)
    {
        _logger.LogInformation("Unity: Processing address sync data from external system");
        // TODO: Synchronize address data — update address information, etc.
        return Task.CompletedTask;
    }

    private Task HandleStatusUpdateData(PluginDataMessage message, MessageContext context)
    {
        _logger.LogInformation("Unity: Processing status update from external system");
        // TODO: Handle status updates — update application status, trigger workflows, etc.
        return Task.CompletedTask;
    }
}
