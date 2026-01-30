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
    /// <param name="cancellationToken">Cancellation token</param>
    Task<Result> PublishAsync(
        string messageType, 
        string messageBody, 
        string? routingKey = null, 
        string? correlationId = null,
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
/// RabbitMQ message publisher implementation
/// </summary>
public class RabbitMQPublisher : IRabbitMQPublisher, IDisposable
{
    private readonly RabbitMQConfiguration _configuration;
    private readonly ILogger<RabbitMQPublisher> _logger;
    private readonly IConnection? _connection;
    private readonly IModel? _channel;
    private bool _disposed = false;

    public RabbitMQPublisher(RabbitMQConfiguration configuration, ILogger<RabbitMQPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;

        try
        {
            // Create connection factory
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

            // Create connection and channel
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ publisher");
            Dispose();
            throw;
        }
    }

    public async Task<Result> PublishAsync(
        string messageType, 
        string messageBody, 
        string? routingKey = null, 
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        if (_disposed || _channel == null)
        {
            return Result.Error("RabbitMQ publisher is not available");
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
            properties.MessageId = Guid.NewGuid().ToString();
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
        if (_disposed || _channel == null)
        {
            return Result.Error("RabbitMQ publisher is not available");
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
                var result = await PublishAsync(messageType, messageBody, routingKey, correlationId, cancellationToken);
                
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

        try
        {
            _channel?.Close();
            _channel?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error closing RabbitMQ channel");
        }

        try
        {
            _connection?.Close();
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error closing RabbitMQ connection");
        }

        _disposed = true;
        _logger.LogDebug("RabbitMQ publisher disposed");
    }
}
