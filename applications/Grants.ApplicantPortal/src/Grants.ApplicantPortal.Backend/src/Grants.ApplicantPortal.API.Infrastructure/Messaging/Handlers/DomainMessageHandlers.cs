using Grants.ApplicantPortal.API.Infrastructure.Messaging.Abstractions;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Messages;

namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.Handlers;

/// <summary>
/// Handler for ProfileUpdatedMessage - logs the profile update
/// </summary>
public class ProfileUpdatedMessageHandler : IMessageHandler<ProfileUpdatedMessage>
{
    private readonly ILogger<ProfileUpdatedMessageHandler> _logger;

    public ProfileUpdatedMessageHandler(ILogger<ProfileUpdatedMessageHandler> logger)
    {
        _logger = logger;
    }

    public async Task<Result> HandleAsync(ProfileUpdatedMessage message, MessageContext context)
    {
        try
        {
            _logger.LogInformation("Profile {ProfileId} was updated by plugin {PluginId} for provider {Provider}, key {Key}",
                message.ProfileId, message.PluginId, message.Provider, message.Key);

            // TODO: Add your business logic here
            // Examples:
            // - Send notifications
            // - Update search indexes
            // - Trigger workflows
            // - Update caches
            
            await Task.Delay(10, context.CancellationToken); // Simulate some work

            _logger.LogDebug("Successfully processed ProfileUpdatedMessage for {ProfileId}", message.ProfileId);
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling ProfileUpdatedMessage for {ProfileId}", message.ProfileId);
            return Result.Error($"Failed to handle ProfileUpdatedMessage: {ex.Message}");
        }
    }
}

/// <summary>
/// Handler for ContactCreatedMessage - logs the contact creation
/// </summary>
public class ContactCreatedMessageHandler : IMessageHandler<ContactCreatedMessage>
{
    private readonly ILogger<ContactCreatedMessageHandler> _logger;

    public ContactCreatedMessageHandler(ILogger<ContactCreatedMessageHandler> logger)
    {
        _logger = logger;
    }

    public async Task<Result> HandleAsync(ContactCreatedMessage message, MessageContext context)
    {
        try
        {
            _logger.LogInformation("Contact {ContactId} '{ContactName}' of type {ContactType} was created by plugin {PluginId} for profile {ProfileId}",
                message.ContactId, message.ContactName, message.ContactType, message.PluginId, message.ProfileId);

            // TODO: Add your business logic here
            // Examples:
            // - Send welcome emails
            // - Update contact directories
            // - Trigger approval workflows
            // - Update user permissions
            
            await Task.Delay(10, context.CancellationToken); // Simulate some work

            _logger.LogDebug("Successfully processed ContactCreatedMessage for contact {ContactId}", message.ContactId);
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling ContactCreatedMessage for contact {ContactId}", message.ContactId);
            return Result.Error($"Failed to handle ContactCreatedMessage: {ex.Message}");
        }
    }
}

/// <summary>
/// Handler for SystemEventMessage - logs system events
/// </summary>
public class SystemEventMessageHandler : IMessageHandler<SystemEventMessage>
{
    private readonly ILogger<SystemEventMessageHandler> _logger;

    public SystemEventMessageHandler(ILogger<SystemEventMessageHandler> logger)
    {
        _logger = logger;
    }

    public async Task<Result> HandleAsync(SystemEventMessage message, MessageContext context)
    {
        try
        {
            _logger.LogInformation("System event '{EventType}': {EventDescription}",
                message.EventType, message.EventDescription);

            if (message.EventData != null)
            {
                _logger.LogDebug("System event data: {EventData}", message.EventData);
            }

            // TODO: Add your business logic here
            // Examples:
            // - Send alerts for critical events
            // - Update monitoring dashboards
            // - Trigger automated responses
            // - Log to external systems
            
            await Task.Delay(5, context.CancellationToken); // Simulate some work

            _logger.LogDebug("Successfully processed SystemEventMessage of type {EventType}", message.EventType);
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling SystemEventMessage of type {EventType}", message.EventType);
            return Result.Error($"Failed to handle SystemEventMessage: {ex.Message}");
        }
    }
}
