using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Inbox;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.RabbitMQ;

namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.RabbitMQ;

/// <summary>
/// RabbitMQ consumer that receives messages and stores them in the inbox
/// </summary>
public class RabbitMQConsumer : IDisposable
{
    private readonly IInboxRepository _inboxRepository;
    private readonly RabbitMQConfiguration _configuration;
    private readonly ILogger<RabbitMQConsumer> _logger;
    private readonly IConnection? _connection;
    private readonly IModel? _channel;
    private bool _disposed = false;
    private string? _consumerTag;

    public RabbitMQConsumer(
        IInboxRepository inboxRepository,
        RabbitMQConfiguration configuration,
        ILogger<RabbitMQConsumer> logger)
    {
        _inboxRepository = inboxRepository;
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
            _connection = factory.CreateConnection("Grants-ApplicantPortal-Consumer");
            _channel = _connection.CreateModel();

            // Declare exchange if configured to do so
            if (_configuration.DeclareExchange)
            {
                _channel.ExchangeDeclare(
                    exchange: _configuration.DefaultExchange,
                    type: _configuration.ExchangeType,
                    durable: _configuration.ExchangeDurable,
                    autoDelete: false);
            }

            // Declare queue if configured to do so
            if (_configuration.DeclareQueue)
            {
                var queueArguments = new Dictionary<string, object>();
                if (_configuration.UseQuorumQueues)
                {
                    queueArguments["x-queue-type"] = "quorum";
                }

                _channel.QueueDeclare(
                    queue: _configuration.DefaultQueue,
                    durable: _configuration.UseQuorumQueues || _configuration.QueueDurable,
                    exclusive: false,
                    autoDelete: _configuration.UseQuorumQueues ? false : _configuration.QueueAutoDelete,
                    arguments: queueArguments);

                // Bind queue to exchange with routing patterns
                var routingKeys = new[] { "grants.*.#", "system.*.#" }; // Listen to all grants messages
                foreach (var routingKey in routingKeys)
                {
                    _channel.QueueBind(
                        queue: _configuration.DefaultQueue,
                        exchange: _configuration.DefaultExchange,
                        routingKey: routingKey);
                }

                _logger.LogDebug("Bound queue {Queue} to exchange {Exchange} with routing keys: {RoutingKeys}", 
                    _configuration.DefaultQueue, _configuration.DefaultExchange, string.Join(", ", routingKeys));
            }

            // Set up quality of service (only process one message at a time)
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            _logger.LogInformation("RabbitMQ consumer connected to {Host}:{Port}, queue: {Queue}", 
                _configuration.HostName, _configuration.Port, _configuration.DefaultQueue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ consumer");
            Dispose();
            throw;
        }
    }

    /// <summary>
    /// Starts consuming messages from RabbitMQ
    /// </summary>
    public void StartConsuming()
    {
        if (_disposed || _channel == null)
        {
            throw new InvalidOperationException("RabbitMQ consumer is not available");
        }

        try
        {
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                await ProcessReceivedMessage(ea);
            };

            _consumerTag = _channel.BasicConsume(
                queue: _configuration.DefaultQueue,
                autoAck: false, // We'll manually ack after storing in inbox
                consumer: consumer);

            _logger.LogInformation("Started consuming messages from queue {Queue} with tag {ConsumerTag}", 
                _configuration.DefaultQueue, _consumerTag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start consuming messages");
            throw;
        }
    }

    /// <summary>
    /// Stops consuming messages from RabbitMQ
    /// </summary>
    public void StopConsuming()
    {
        if (!string.IsNullOrEmpty(_consumerTag) && _channel != null && !_disposed)
        {
            try
            {
                _channel.BasicCancel(_consumerTag);
                _logger.LogInformation("Stopped consuming messages with tag {ConsumerTag}", _consumerTag);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error stopping consumer {ConsumerTag}", _consumerTag);
            }
        }
    }

    /// <summary>
    /// Processes a received message by storing it in the inbox
    /// </summary>
    private async Task ProcessReceivedMessage(BasicDeliverEventArgs ea)
    {
        var messageId = Guid.Empty;
        var messageType = "Unknown";

        try
        {
            // Extract message metadata
            messageId = Guid.TryParse(ea.BasicProperties.MessageId, out var id) ? id : Guid.NewGuid();
            messageType = ea.BasicProperties.Type ?? "Unknown";
            var correlationId = ea.BasicProperties.CorrelationId;
            var routingKey = ea.RoutingKey;

            // Get message body
            var messageBody = Encoding.UTF8.GetString(ea.Body.Span);

            _logger.LogDebug("Received message {MessageId} of type {MessageType} with routing key {RoutingKey}", 
                messageId, messageType, routingKey);

            // Validate message size
            if (messageBody.Length > _configuration.MaxMessageSize)
            {
                _logger.LogError("Message {MessageId} exceeds maximum size ({Size} > {MaxSize})", 
                    messageId, messageBody.Length, _configuration.MaxMessageSize);
                
                // Reject and don't requeue oversized messages
                _channel?.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                return;
            }

            // Create inbox message
            var inboxMessage = new InboxMessage(messageId, messageType, messageBody, correlationId);

            // Store in inbox repository (with duplicate detection)
            var storeResult = await _inboxRepository.AddAsync(inboxMessage);

            if (storeResult.IsSuccess)
            {
                // Successfully stored - acknowledge the message
                _channel?.BasicAck(ea.DeliveryTag, multiple: false);
                
                _logger.LogDebug("Successfully stored message {MessageId} in inbox and acknowledged RabbitMQ delivery", 
                    messageId);
            }
            else
            {
                // Failed to store - reject and requeue for retry
                _logger.LogError("Failed to store message {MessageId} in inbox: {Errors}", 
                    messageId, string.Join(", ", storeResult.Errors));
                
                _channel?.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        }
        catch (JsonException ex)
        {
            // Invalid JSON - reject without requeue
            _logger.LogError(ex, "Invalid JSON in message {MessageId} of type {MessageType}", messageId, messageType);
            _channel?.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
        }
        catch (Exception ex)
        {
            // Unexpected error - reject and requeue for retry
            _logger.LogError(ex, "Error processing message {MessageId} of type {MessageType}", messageId, messageType);
            _channel?.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        StopConsuming();

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
        _logger.LogDebug("RabbitMQ consumer disposed");
    }
}
