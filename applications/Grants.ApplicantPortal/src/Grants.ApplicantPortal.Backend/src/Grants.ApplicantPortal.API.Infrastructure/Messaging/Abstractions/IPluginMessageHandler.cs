namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.Abstractions;

/// <summary>
/// Interface that plugins can implement to handle incoming messages and acknowledgments
/// </summary>
public interface IPluginMessageHandler
{
    /// <summary>
    /// Plugin ID that this handler supports
    /// </summary>
    string PluginId { get; }
    
    /// <summary>
    /// Handle acknowledgment for a message sent by this plugin
    /// </summary>
    /// <param name="acknowledgment">The acknowledgment message</param>
    /// <param name="context">Message processing context</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> HandleAcknowledgmentAsync(Messages.MessageAcknowledgment acknowledgment, MessageContext context);
    
    /// <summary>
    /// Handle incoming data message for this plugin
    /// </summary>
    /// <param name="message">The incoming plugin data message</param>
    /// <param name="context">Message processing context</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> HandleIncomingDataAsync(Messages.PluginDataMessage message, MessageContext context);

    /// <summary>
    /// Checks if this handler can process the given message
    /// </summary>
    /// <param name="message">The message to check</param>
    /// <returns>True if this handler can process the message</returns>
    bool CanHandle(IMessage message);
}

/// <summary>
/// Interface for routing messages to plugin-specific handlers
/// </summary>
public interface IPluginMessageRouter
{
    /// <summary>
    /// Routes a message to the appropriate plugin handler
    /// </summary>
    /// <param name="message">The message to route</param>
    /// <param name="context">Message processing context</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> RouteToPluginAsync(IMessage message, MessageContext context);

    /// <summary>
    /// Gets all registered plugin handlers
    /// </summary>
    /// <returns>Collection of plugin handlers</returns>
    IEnumerable<IPluginMessageHandler> GetAllPluginHandlers();

    /// <summary>
    /// Gets the handler for a specific plugin ID
    /// </summary>
    /// <param name="pluginId">Plugin ID to find handler for</param>
    /// <returns>Plugin handler if found, null otherwise</returns>
    IPluginMessageHandler? GetPluginHandler(string pluginId);
}
