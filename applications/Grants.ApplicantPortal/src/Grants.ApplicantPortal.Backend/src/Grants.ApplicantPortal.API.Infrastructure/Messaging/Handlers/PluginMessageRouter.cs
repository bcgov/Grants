using Grants.ApplicantPortal.API.Infrastructure.Messaging.Abstractions;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Messages;

namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.Handlers;

/// <summary>
/// Router that handles plugin-specific message routing for acknowledgments and data messages
/// </summary>
public class PluginMessageRouter : IPluginMessageRouter
{
    private readonly IEnumerable<IPluginMessageHandler> _pluginHandlers;
    private readonly ILogger<PluginMessageRouter> _logger;

    public PluginMessageRouter(IEnumerable<IPluginMessageHandler> pluginHandlers, ILogger<PluginMessageRouter> logger)
    {
        _pluginHandlers = pluginHandlers;
        _logger = logger;
    }

    public async Task<Result> RouteToPluginAsync(IMessage message, MessageContext context)
    {
        try
        {
            // Route based on message type
            return message switch
            {
                MessageAcknowledgment ack => await RouteAcknowledgmentAsync(ack, context),
                PluginDataMessage data => await RouteDataMessageAsync(data, context),
                _ => await RouteGenericMessageAsync(message, context)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing message {MessageId} of type {MessageType}", 
                message.MessageId, message.MessageType);
            return Result.Error($"Failed to route message: {ex.Message}");
        }
    }

    public IEnumerable<IPluginMessageHandler> GetAllPluginHandlers()
    {
        return _pluginHandlers;
    }

    public IPluginMessageHandler? GetPluginHandler(string pluginId)
    {
        return _pluginHandlers.FirstOrDefault(h => h.PluginId.Equals(pluginId, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<Result> RouteAcknowledgmentAsync(MessageAcknowledgment acknowledgment, MessageContext context)
    {
        var handler = GetPluginHandler(acknowledgment.PluginId ?? "");
        
        if (handler == null)
        {
            _logger.LogWarning("No plugin handler found for acknowledgment from plugin {PluginId}", acknowledgment.PluginId);
            return Result.Error($"No handler found for plugin {acknowledgment.PluginId}");
        }

        _logger.LogDebug("Routing acknowledgment {MessageId} to plugin handler {PluginId}", 
            acknowledgment.MessageId, acknowledgment.PluginId);

        return await handler.HandleAcknowledgmentAsync(acknowledgment, context);
    }

    private async Task<Result> RouteDataMessageAsync(PluginDataMessage dataMessage, MessageContext context)
    {
        var handler = GetPluginHandler(dataMessage.PluginId ?? "");
        
        if (handler == null)
        {
            _logger.LogWarning("No plugin handler found for data message from plugin {PluginId}", dataMessage.PluginId);
            return Result.Error($"No handler found for plugin {dataMessage.PluginId}");
        }

        _logger.LogDebug("Routing data message {MessageId} to plugin handler {PluginId}", 
            dataMessage.MessageId, dataMessage.PluginId);

        return await handler.HandleIncomingDataAsync(dataMessage, context);
    }

    private async Task<Result> RouteGenericMessageAsync(IMessage message, MessageContext context)
    {
        // For generic messages, find a handler that can process it
        var applicableHandlers = _pluginHandlers.Where(h => h.CanHandle(message)).ToList();

        if (!applicableHandlers.Any())
        {
            _logger.LogWarning("No plugin handlers found for message {MessageId} of type {MessageType}", 
                message.MessageId, message.MessageType);
            return Result.Error($"No handlers found for message type {message.MessageType}");
        }

        var results = new List<Result>();

        foreach (var handler in applicableHandlers)
        {
            try
            {
                _logger.LogDebug("Routing message {MessageId} to plugin handler {PluginId}", 
                    message.MessageId, handler.PluginId);

                // For generic messages, try to handle as data message if it has plugin context
                if (message.PluginId != null && message is IMessage genericMessage)
                {
                    // Convert to plugin data message for generic handling
                    var pluginDataMessage = new PluginDataMessage(
                        message.PluginId, 
                        message.MessageType,
                        genericMessage,
                        message.CorrelationId);
                    
                    var result = await handler.HandleIncomingDataAsync(pluginDataMessage, context);
                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling message {MessageId} with plugin handler {PluginId}", 
                    message.MessageId, handler.PluginId);
                results.Add(Result.Error($"Handler {handler.PluginId} failed: {ex.Message}"));
            }
        }

        // Return success if any handler succeeded, otherwise return combined errors
        var successful = results.Where(r => r.IsSuccess).ToList();
        if (successful.Any())
        {
            _logger.LogDebug("Successfully processed message {MessageId} with {Count} handlers", 
                message.MessageId, successful.Count);
            return Result.Success();
        }

        var errors = results.SelectMany(r => r.Errors).ToList();
        if (errors.Any())
        {
            return Result.Error(string.Join("; ", errors));
        }
        
        return Result.Error("All handlers failed with unknown errors");
    }
}
