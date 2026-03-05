using Grants.ApplicantPortal.API.Infrastructure.Messaging.Configuration;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Inbox;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Outbox;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.Jobs;

/// <summary>
/// Nightly cleanup job that removes processed inbox and outbox messages
/// older than the configured retention period (default: 7 days).
/// Only deletes messages that have been fully processed (Published/Completed).
/// </summary>
[DisallowConcurrentExecution]
public class MessageCleanupJob : IJob
{
    private readonly IOutboxRepository _outboxRepository;
    private readonly IInboxRepository _inboxRepository;
    private readonly MessagingOptions _options;
    private readonly ILogger<MessageCleanupJob> _logger;

    public MessageCleanupJob(
        IOutboxRepository outboxRepository,
        IInboxRepository inboxRepository,
        IOptions<MessagingOptions> options,
        ILogger<MessageCleanupJob> logger)
    {
        _outboxRepository = outboxRepository;
        _inboxRepository = inboxRepository;
        _options = options.Value;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;

        _logger.LogInformation("Message cleanup job starting");

        var outboxCutoff = DateTime.UtcNow.AddDays(-_options.Outbox.RetentionDays);
        var inboxCutoff = DateTime.UtcNow.AddDays(-_options.Inbox.RetentionDays);

        try
        {
            var outboxDeleted = await _outboxRepository.CleanupOldMessagesAsync(outboxCutoff, cancellationToken);
            var inboxDeleted = await _inboxRepository.CleanupOldMessagesAsync(inboxCutoff, cancellationToken);

            if (outboxDeleted > 0 || inboxDeleted > 0)
            {
                _logger.LogInformation(
                    "Message cleanup completed. Outbox: {OutboxDeleted} removed (older than {OutboxDays}d), Inbox: {InboxDeleted} removed (older than {InboxDays}d)",
                    outboxDeleted, _options.Outbox.RetentionDays,
                    inboxDeleted, _options.Inbox.RetentionDays);
            }
            else
            {
                _logger.LogDebug("Message cleanup completed. No messages to remove");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during message cleanup");
        }
    }
}
