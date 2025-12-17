namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.Abstractions;

/// <summary>
/// Interface for publishing messages to the outbox
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Publishes a message to the outbox for later processing
    /// </summary>
    /// <param name="message">The message to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> PublishAsync<T>(T message, CancellationToken cancellationToken = default) 
        where T : class, IMessage;

    /// <summary>
    /// Publishes multiple messages as a single atomic operation
    /// </summary>
    /// <param name="messages">The messages to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> PublishBatchAsync(IEnumerable<IMessage> messages, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for handling incoming messages
/// </summary>
public interface IMessageHandler<in T> where T : class, IMessage
{
    /// <summary>
    /// Handles the incoming message
    /// </summary>
    /// <param name="message">The message to handle</param>
    /// <param name="context">Message processing context</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> HandleAsync(T message, MessageContext context);
}

/// <summary>
/// Interface for routing messages to appropriate handlers
/// </summary>
public interface IMessageHandlerResolver
{
    /// <summary>
    /// Gets all handlers for a specific message type
    /// </summary>
    /// <param name="messageType">The type of the message</param>
    /// <returns>Collection of handlers that can process this message type</returns>
    IEnumerable<object> GetHandlers(Type messageType);

    /// <summary>
    /// Gets all handlers for a specific message
    /// </summary>
    /// <param name="message">The message instance</param>
    /// <returns>Collection of handlers that can process this message</returns>
    IEnumerable<object> GetHandlers(IMessage message);
}
