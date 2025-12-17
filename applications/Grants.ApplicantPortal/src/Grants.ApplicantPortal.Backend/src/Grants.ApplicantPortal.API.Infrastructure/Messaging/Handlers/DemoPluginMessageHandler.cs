using Grants.ApplicantPortal.API.Infrastructure.Messaging.Abstractions;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Messages;

namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.Handlers;

/// <summary>
/// Demo plugin message handler for testing acknowledgments and data messages
/// </summary>
public class DemoPluginMessageHandler : IPluginMessageHandler
{
    private readonly ILogger<DemoPluginMessageHandler> _logger;

    public DemoPluginMessageHandler(ILogger<DemoPluginMessageHandler> logger)
    {
        _logger = logger;
    }

    public string PluginId => "DEMO";

    public async Task<Result> HandleAcknowledgmentAsync(MessageAcknowledgment acknowledgment, MessageContext context)
    {
        try
        {
            _logger.LogInformation("Demo plugin received acknowledgment for message {OriginalMessageId} with status {Status}", 
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
                    _logger.LogWarning("Unknown acknowledgment status {Status} for message {OriginalMessageId}", 
                        acknowledgment.Status, acknowledgment.OriginalMessageId);
                    break;
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling acknowledgment for message {OriginalMessageId}", 
                acknowledgment.OriginalMessageId);
            return Result.Error($"Failed to handle acknowledgment: {ex.Message}");
        }
    }

    public async Task<Result> HandleIncomingDataAsync(PluginDataMessage message, MessageContext context)
    {
        try
        {
            _logger.LogInformation("Demo plugin received data message of type {DataType}", message.DataType);

            // Handle different types of incoming data
            switch (message.DataType.ToUpperInvariant())
            {
                case "PROFILE_UPDATE":
                    await HandleProfileUpdateData(message, context);
                    break;
                case "CONTACT_SYNC":
                    await HandleContactSyncData(message, context);
                    break;
                case "STATUS_UPDATE":
                    await HandleStatusUpdateData(message, context);
                    break;
                default:
                    _logger.LogInformation("Demo plugin received unknown data type {DataType}, logging for reference", 
                        message.DataType);
                    _logger.LogDebug("Unknown data message: {@Data}", message.Data);
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
        // Handle messages specifically for DEMO plugin or generic messages without plugin ID
        return message.PluginId == null || message.PluginId.Equals(PluginId, StringComparison.OrdinalIgnoreCase);
    }

    private async Task HandleSuccessfulAcknowledgment(MessageAcknowledgment acknowledgment, MessageContext context)
    {
        _logger.LogInformation("Demo plugin: External system successfully processed message {OriginalMessageId}", 
            acknowledgment.OriginalMessageId);
        
        // TODO: Update internal state, clear pending operations, etc.
        // For example: Mark the operation as completed in local cache
        
        await Task.Delay(10, context.CancellationToken); // Simulate some work
    }

    private async Task HandleFailedAcknowledgment(MessageAcknowledgment acknowledgment, MessageContext context)
    {
        _logger.LogWarning("Demo plugin: External system failed to process message {OriginalMessageId}, details: {Details}", 
            acknowledgment.OriginalMessageId, acknowledgment.Details);
        
        // TODO: Handle the failure - retry, alert, compensation logic, etc.
        // For example: Schedule retry, send notification to administrators
        
        await Task.Delay(10, context.CancellationToken); // Simulate some work
    }

    private async Task HandleProcessingAcknowledgment(MessageAcknowledgment acknowledgment, MessageContext context)
    {
        _logger.LogInformation("Demo plugin: External system is still processing message {OriginalMessageId}", 
            acknowledgment.OriginalMessageId);
        
        // TODO: Update status indicators, extend timeouts, etc.
        // For example: Update progress indicators, extend operation timeout
        
        await Task.Delay(5, context.CancellationToken); // Simulate some work
    }

    private async Task HandleProfileUpdateData(PluginDataMessage message, MessageContext context)
    {
        _logger.LogInformation("Demo plugin: Processing profile update data from external system");
        
        // TODO: Process the profile update from external system
        // For example: Update local profile data, trigger UI refresh, etc.
        
        await Task.Delay(20, context.CancellationToken); // Simulate some work
    }

    private async Task HandleContactSyncData(PluginDataMessage message, MessageContext context)
    {
        _logger.LogInformation("Demo plugin: Processing contact sync data from external system");
        
        // TODO: Synchronize contact data from external system
        // For example: Update contact information, merge duplicates, etc.
        
        await Task.Delay(15, context.CancellationToken); // Simulate some work
    }

    private async Task HandleStatusUpdateData(PluginDataMessage message, MessageContext context)
    {
        _logger.LogInformation("Demo plugin: Processing status update from external system");
        
        // TODO: Handle status updates from external system
        // For example: Update application status, trigger workflows, etc.
        
        await Task.Delay(10, context.CancellationToken); // Simulate some work
    }
}
