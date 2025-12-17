using Grants.ApplicantPortal.API.Infrastructure.Messaging.Abstractions;

namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.Handlers;

/// <summary>
/// Default message handler resolver that uses dependency injection
/// </summary>
public class ServiceProviderMessageHandlerResolver : IMessageHandlerResolver
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ServiceProviderMessageHandlerResolver> _logger;

    public ServiceProviderMessageHandlerResolver(IServiceProvider serviceProvider, ILogger<ServiceProviderMessageHandlerResolver> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public IEnumerable<object> GetHandlers(Type messageType)
    {
        try
        {
            // Create the generic handler interface type
            var handlerType = typeof(IMessageHandler<>).MakeGenericType(messageType);
            
            // Get all registered handlers for this message type
            var handlers = _serviceProvider.GetServices(handlerType);
            
            _logger.LogDebug("Found {Count} handlers for message type {MessageType}", 
                handlers.Count(), messageType.Name);
            
            return handlers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting handlers for message type {MessageType}", messageType.Name);
            return Enumerable.Empty<object>();
        }
    }

    public IEnumerable<object> GetHandlers(IMessage message)
    {
        return GetHandlers(message.GetType());
    }
}
