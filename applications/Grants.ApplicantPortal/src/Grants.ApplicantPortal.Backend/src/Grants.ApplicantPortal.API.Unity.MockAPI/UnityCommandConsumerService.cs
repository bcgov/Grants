using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Grants.ApplicantPortal.API.Unity.MockAPI;

/// <summary>
/// Background service that consumes command messages from RabbitMQ (sent by the Grants Applicant Portal outbox)
/// and publishes acknowledgment messages back to RabbitMQ (consumed by the Portal inbox).
/// 
/// This simulates the real Unity system's message processing behaviour for local development.
/// </summary>
public sealed class UnityCommandConsumerService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<UnityCommandConsumerService> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public UnityCommandConsumerService(
        IConfiguration configuration,
        ILogger<UnityCommandConsumerService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var hostName = _configuration["RabbitMQ:HostName"] ?? "localhost";
        var port = _configuration.GetValue("RabbitMQ:Port", 5672);
        var userName = _configuration["RabbitMQ:UserName"] ?? "guest";
        var password = _configuration["RabbitMQ:Password"] ?? "guest";
        var virtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/";
        var exchange = _configuration["RabbitMQ:Exchange"] ?? "grants.messaging";
        var exchangeType = _configuration["RabbitMQ:ExchangeType"] ?? "topic";
        var queue = _configuration["RabbitMQ:InboundQueue"] ?? "unity.mockapi.commands";
        var routingKeys = _configuration.GetSection("RabbitMQ:InboundRoutingKeys").Get<string[]>()
                          ?? ["commands.unity.plugindata"];
        var ackRoutingKey = _configuration["RabbitMQ:AckRoutingKey"] ?? "grants.unity.acknowledgment";

        // Retry connection with backoff
        var connected = false;
        var attempt = 0;

        while (!connected && !stoppingToken.IsCancellationRequested)
        {
            try
            {
                attempt++;
                _logger.LogInformation(
                    "Connecting to RabbitMQ at {Host}:{Port} (attempt {Attempt})...",
                    hostName, port, attempt);

                var factory = new ConnectionFactory
                {
                    HostName = hostName,
                    Port = port,
                    UserName = userName,
                    Password = password,
                    VirtualHost = virtualHost,
                    RequestedConnectionTimeout = TimeSpan.FromSeconds(15)
                };

                _connection = factory.CreateConnection("Unity-MockAPI-Consumer");
                _channel = _connection.CreateModel();

                // Declare exchange (idempotent)
                _channel.ExchangeDeclare(
                    exchange: exchange,
                    type: exchangeType,
                    durable: true,
                    autoDelete: false);

                // Declare our own queue for Unity commands
                _channel.QueueDeclare(
                    queue: queue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false);

                // Bind with routing patterns
                foreach (var rk in routingKeys)
                {
                    _channel.QueueBind(queue: queue, exchange: exchange, routingKey: rk);
                    _logger.LogInformation("Bound queue {Queue} → exchange {Exchange} with routing key {RoutingKey}",
                        queue, exchange, rk);
                }

                _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                connected = true;
                _logger.LogInformation("RabbitMQ consumer connected successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to connect to RabbitMQ (attempt {Attempt}). Retrying in 5 seconds...", attempt);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        if (_channel == null || stoppingToken.IsCancellationRequested)
        {
            return;
        }

        // Start consuming
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (_, ea) =>
        {
            _ = ProcessMessageAsync(ea, exchange, ackRoutingKey);
        };

        var consumerTag = _channel.BasicConsume(queue: queue, autoAck: false, consumer: consumer);
        _logger.LogInformation("Started consuming from queue {Queue} with tag {Tag}", queue, consumerTag);

        // Keep alive until cancellation
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown
        }
    }

    /// <summary>
    /// Processes a received command message, simulates handling, and publishes an acknowledgment.
    /// </summary>
    private async Task ProcessMessageAsync(BasicDeliverEventArgs ea, string exchange, string ackRoutingKey)
    {
        var messageId = ea.BasicProperties.MessageId ?? Guid.NewGuid().ToString();
        var messageType = ea.BasicProperties.Type ?? "Unknown";
        var correlationId = ea.BasicProperties.CorrelationId;

        try
        {
            // Guard: skip acknowledgment messages to prevent infinite loops.
            // This can happen if the queue still has a stale grants.unity.# binding on the broker.
            if (messageType.Equals("MessageAcknowledgment", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug(
                    "Skipping acknowledgment message {MessageId} (not a command) — acking and discarding",
                    messageId);
                _channel?.BasicAck(ea.DeliveryTag, multiple: false);
                return;
            }

            var body = Encoding.UTF8.GetString(ea.Body.Span);

            _logger.LogInformation(
                "Received command: MessageId={MessageId}, Type={MessageType}, RoutingKey={RoutingKey}",
                messageId, messageType, ea.RoutingKey);
            _logger.LogDebug("Payload: {Body}", body);

            // Parse the command payload
            using var doc = JsonDocument.Parse(body);

            var action = "Unknown";
            if (doc.RootElement.TryGetProperty("dataType", out var dt))
            {
                action = dt.GetString() ?? action;
            }
            else if (doc.RootElement.TryGetProperty("DataType", out var dt2))
            {
                action = dt2.GetString() ?? action;
            }

            // Simulate processing time (50-200 ms)
            var processingMs = Random.Shared.Next(50, 200);
            await Task.Delay(processingMs);

            // Determine outcome — 80% success, 20% failure for realistic simulation
            var roll = Random.Shared.Next(100);
            var succeeded = roll < 80;
            string status;
            string details;

            if (succeeded)
            {
                status = "SUCCESS";
                details = $"Mock Unity processed {action} successfully in {processingMs}ms";
            }
            else
            {
                status = "FAILED";
                details = roll switch
                {
                    < 85 => $"Validation error: duplicate record detected for {action}",
                    < 90 => $"Conflict: record was modified by another user during {action}",
                    < 95 => $"Service unavailable: downstream system timeout after {processingMs}ms for {action}",
                    _    => $"Internal error: unexpected failure processing {action}"
                };
            }

            _logger.LogInformation(
                "Processed command {MessageId} ({Action}): {Status} — {Details}",
                messageId, action, status, details);

            // Build and publish acknowledgment
            PublishAcknowledgment(exchange, ackRoutingKey, messageId, status, details, correlationId);

            // Ack the original message
            _channel?.BasicAck(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing command {MessageId}", messageId);

            // Publish a FAILED acknowledgment
            PublishAcknowledgment(exchange, ackRoutingKey, messageId, "FAILED",
                $"Processing error: {ex.Message}", correlationId);

            // Ack to remove from queue (we've already sent a FAILED ack)
            _channel?.BasicAck(ea.DeliveryTag, multiple: false);
        }
    }

    /// <summary>
    /// Publishes a MessageAcknowledgment message back to the grants exchange.
    /// </summary>
    private void PublishAcknowledgment(
        string exchange,
        string routingKey,
        string originalMessageId,
        string status,
        string? details,
        string? correlationId)
    {
        if (_channel == null)
        {
            _logger.LogError("Cannot publish acknowledgment — channel is null");
            return;
        }

        try
        {
            var ack = new
            {
                messageId = Guid.NewGuid(),
                messageType = "MessageAcknowledgment",
                createdAt = DateTime.UtcNow,
                correlationId,
                pluginId = "UNITY",
                originalMessageId,
                status,
                details,
                processedAt = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(ack, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            var body = Encoding.UTF8.GetBytes(json);

            var props = _channel.CreateBasicProperties();
            props.Persistent = true;
            props.MessageId = ack.messageId.ToString();
            props.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            props.Type = "MessageAcknowledgment";
            props.ContentType = "application/json";
            props.ContentEncoding = "utf-8";

            if (!string.IsNullOrEmpty(correlationId))
            {
                props.CorrelationId = correlationId;
            }

            _channel.BasicPublish(
                exchange: exchange,
                routingKey: routingKey,
                basicProperties: props,
                body: body);

            _logger.LogInformation(
                "Published acknowledgment for {OriginalMessageId}: Status={Status}, RoutingKey={RoutingKey}",
                originalMessageId, status, routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish acknowledgment for {OriginalMessageId}", originalMessageId);
        }
    }

    public override void Dispose()
    {
        try { _channel?.Close(); _channel?.Dispose(); }
        catch { /* best effort */ }

        try { _connection?.Close(); _connection?.Dispose(); }
        catch { /* best effort */ }

        base.Dispose();
    }
}
