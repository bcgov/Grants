using System.Text.Json;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Abstractions;

namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.Outbox;

/// <summary>
/// Message publisher implementation that uses the Outbox pattern
/// </summary>
public class OutboxMessagePublisher : IMessagePublisher
{
    private readonly IOutboxRepository _outboxRepository;
    private readonly ILogger<OutboxMessagePublisher> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public OutboxMessagePublisher(
        IOutboxRepository outboxRepository,
        ILogger<OutboxMessagePublisher> logger)
    {
        _outboxRepository = outboxRepository;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<Result> PublishAsync<T>(T message, CancellationToken cancellationToken = default) 
        where T : class, IMessage
    {
        try
        {
            _logger.LogDebug("Publishing message {MessageId} of type {MessageType}", 
                message.MessageId, message.MessageType);

            var payload = JsonSerializer.Serialize(message, _jsonOptions);

            var outboxMessage = new OutboxMessage(
                message.MessageId,
                message.MessageType,
                payload,
                message.PluginId,
                message.CorrelationId);

            var result = await _outboxRepository.AddAsync(outboxMessage, cancellationToken);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully published message {MessageId} of type {MessageType} to outbox", 
                    message.MessageId, message.MessageType);
            }
            else
            {
                _logger.LogError("Failed to publish message {MessageId} of type {MessageType} to outbox: {Error}", 
                    message.MessageId, message.MessageType, string.Join(", ", result.Errors));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error publishing message {MessageId} of type {MessageType}", 
                message.MessageId, message.MessageType);
            return Result.Error($"Unexpected error publishing message: {ex.Message}");
        }
    }

    public async Task<Result> PublishBatchAsync(IEnumerable<IMessage> messages, CancellationToken cancellationToken = default)
    {
        var messagesList = messages.ToList();
        if (!messagesList.Any())
        {
            return Result.Success();
        }

        try
        {
            _logger.LogDebug("Publishing batch of {Count} messages to outbox", messagesList.Count);

            var outboxMessages = messagesList.Select(message =>
            {
                var payload = JsonSerializer.Serialize(message, message.GetType(), _jsonOptions);
                return new OutboxMessage(
                    message.MessageId,
                    message.MessageType,
                    payload,
                    message.PluginId,
                    message.CorrelationId);
            });

            var result = await _outboxRepository.AddBatchAsync(outboxMessages, cancellationToken);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("Successfully published batch of {Count} messages to outbox", messagesList.Count);
            }
            else
            {
                _logger.LogError("Failed to publish batch of {Count} messages to outbox: {Error}", 
                    messagesList.Count, string.Join(", ", result.Errors));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error publishing batch of {Count} messages", messagesList.Count);
            return Result.Error($"Unexpected error publishing batch of messages: {ex.Message}");
        }
    }
}
