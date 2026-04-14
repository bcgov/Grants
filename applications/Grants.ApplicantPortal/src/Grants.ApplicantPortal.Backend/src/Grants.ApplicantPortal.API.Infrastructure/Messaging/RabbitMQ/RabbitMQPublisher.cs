using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.RabbitMQ;

/// <summary>
/// Interface for publishing messages to RabbitMQ
/// </summary>
public interface IRabbitMQPublisher
{
    /// <summary>
    /// Publishes a message to RabbitMQ
    /// </summary>
    /// <param name="messageType">Type of the message for routing</param>
    /// <param name="messageBody">Serialized message body</param>
    /// <param name="routingKey">Routing key for message routing (optional)</param>
    /// <param name="correlationId">Correlation ID for message tracking (optional)</param>
    /// <param name="messageId">Stable message ID set as the AMQP MessageId property. When null a new GUID is generated.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<Result> PublishAsync(
        string messageType, 
        string messageBody, 
        string? routingKey = null, 
        string? correlationId = null,
        Guid? messageId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes multiple messages as a batch
    /// </summary>
    /// <param name="messages">Messages to publish</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<Result> PublishBatchAsync(
        IEnumerable<(string MessageType, string MessageBody, string? RoutingKey, string? CorrelationId)> messages,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// RabbitMQ message publisher implementation.
/// Connection is deferred until the first publish attempt so the application
/// can start even when RabbitMQ is unavailable.
/// </summary>
public class RabbitMQPublisher : IRabbitMQPublisher, IDisposable
{
    private readonly RabbitMQConfiguration _configuration;
    private readonly ILogger<RabbitMQPublisher> _logger;
    private readonly object _connectionLock = new();
    private IConnection? _connection;
    private IModel? _channel;
    private bool _disposed = false;

    public RabbitMQPublisher(RabbitMQConfiguration configuration, ILogger<RabbitMQPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Lazily establishes the RabbitMQ connection and channel on first use.
    /// Returns false if the connection cannot be established.
    /// </summary>
    private bool EnsureConnected()
    {
        if (_disposed) return false;
        if (_channel is { IsOpen: true }) return true;

        lock (_connectionLock)
        {
            // Double-check after acquiring the lock
            if (_disposed) return false;
            if (_channel is { IsOpen: true }) return true;

            // Clean up any previous broken connection
            CleanupConnection();

            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _configuration.HostName,
                    Port = _configuration.Port,
                    UserName = _configuration.UserName,
                    Password = _configuration.Password,
                    VirtualHost = _configuration.VirtualHost,
                    RequestedConnectionTimeout = TimeSpan.FromSeconds(_configuration.ConnectionTimeoutSeconds)
                };

                if (_configuration.UseSsl)
                {
                    factory.Ssl.Enabled = true;
                }

                _connection = factory.CreateConnection("Grants-ApplicantPortal-Publisher");
                _channel = _connection.CreateModel();

                // Configure publisher confirms for reliability
                _channel.ConfirmSelect();

                // Declare exchange if configured to do so
                if (_configuration.DeclareExchange)
                {
                    _channel.ExchangeDeclare(
                        exchange: _configuration.DefaultExchange,
                        type: _configuration.ExchangeType,
                        durable: _configuration.ExchangeDurable,
                        autoDelete: false);

                    _logger.LogDebug("Declared exchange {Exchange} of type {Type}",
                        _configuration.DefaultExchange, _configuration.ExchangeType);
                }

                _logger.LogInformation("RabbitMQ publisher connected to {Host}:{Port}",
                    _configuration.HostName, _configuration.Port);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RabbitMQ publisher could not connect to {Host}:{Port}. Publishing will be unavailable until RabbitMQ is reachable",
                    _configuration.HostName, _configuration.Port);

                CleanupConnection();
                return false;
            }
        }
    }

    private void CleanupConnection()
    {
        try { _channel?.Close(); } catch { /* best-effort */ }
        try { _channel?.Dispose(); } catch { /* best-effort */ }
        _channel = null;

        try { _connection?.Close(); } catch { /* best-effort */ }
        try { _connection?.Dispose(); } catch { /* best-effort */ }
        _connection = null;
    }

    public async Task<Result> PublishAsync(
        string messageType, 
        string messageBody, 
        string? routingKey = null, 
        string? correlationId = null,
        Guid? messageId = null,
        CancellationToken cancellationToken = default)
    {
        if (!EnsureConnected())
        {
            return Result.Error("RabbitMQ publisher is not available - broker may be unreachable");
        }

        try
        {
            // Validate message size
            var messageBytes = Encoding.UTF8.GetBytes(messageBody);
            if (messageBytes.Length > _configuration.MaxMessageSize)
            {
                return Result.Error($"Message size ({messageBytes.Length} bytes) exceeds maximum allowed size ({_configuration.MaxMessageSize} bytes)");
            }

            // Prepare message properties
            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true; // Make message durable
            properties.MessageId = (messageId ?? Guid.NewGuid()).ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            properties.Type = messageType;
            properties.ContentType = "application/json";
            properties.ContentEncoding = "utf-8";

            if (!string.IsNullOrEmpty(correlationId))
            {
                properties.CorrelationId = correlationId;
            }

            // Determine routing key (use message type if not specified)
            var effectiveRoutingKey = routingKey ?? $"message.{messageType.ToLowerInvariant()}";

            // Publish message
            _channel.BasicPublish(
                exchange: _configuration.DefaultExchange,
                routingKey: effectiveRoutingKey,
                basicProperties: properties,
                body: messageBytes);

            // Wait for publisher confirmation
            var confirmed = _channel.WaitForConfirms(_configuration.PublisherConfirmTimeout);
            
            if (confirmed)
            {
                _logger.LogDebug("Published message {MessageId} of type {MessageType} with routing key {RoutingKey}", 
                    properties.MessageId, messageType, effectiveRoutingKey);
                return Result.Success();
            }
            else
            {
                _logger.LogError("Failed to confirm publication of message type {MessageType}", messageType);
                return Result.Error($"Failed to confirm message publication for type {messageType}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing message of type {MessageType}", messageType);
            return Result.Error($"Failed to publish message: {ex.Message}");
        }
    }

    public async Task<Result> PublishBatchAsync(
        IEnumerable<(string MessageType, string MessageBody, string? RoutingKey, string? CorrelationId)> messages,
        CancellationToken cancellationToken = default)
    {
        if (!EnsureConnected())
        {
            return Result.Error("RabbitMQ publisher is not available - broker may be unreachable");
        }

        var messagesList = messages.ToList();
        if (!messagesList.Any())
        {
            return Result.Success();
        }

        try
        {
            var publishedCount = 0;

            foreach (var (messageType, messageBody, routingKey, correlationId) in messagesList)
            {
                var result = await PublishAsync(messageType, messageBody, routingKey, correlationId, messageId: null, cancellationToken);
                
                if (!result.IsSuccess)
                {
                    _logger.LogError("Failed to publish message {Index} of {Total} in batch: {Error}", 
                        publishedCount + 1, messagesList.Count, string.Join(", ", result.Errors));
                    return result;
                }
                
                publishedCount++;
            }

            _logger.LogInformation("Successfully published batch of {Count} messages", publishedCount);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing batch of {Count} messages", messagesList.Count);
            return Result.Error($"Failed to publish message batch: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        lock (_connectionLock)
        {
            if (_disposed) return;
            _disposed = true;
            CleanupConnection();
        }

        _logger.LogDebug("RabbitMQ publisher disposed");
    }
}
