using FluentAssertions;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Outbox;

namespace Grants.ApplicantPortal.API.UnitTests.Infrastructure;

public class OutboxMessageTests
{
    private OutboxMessage CreateMessage(string type = "TestMessage", string payload = "{}")
        => new(Guid.NewGuid(), type, payload, "UNITY", "corr-1");

    [Fact]
    public void Constructor_SetsInitialState()
    {
        var messageId = Guid.NewGuid();
        var msg = new OutboxMessage(messageId, "TestType", "{\"a\":1}", "DEMO", "corr-42");

        msg.MessageId.Should().Be(messageId);
        msg.MessageType.Should().Be("TestType");
        msg.Payload.Should().Be("{\"a\":1}");
        msg.PluginId.Should().Be("DEMO");
        msg.CorrelationId.Should().Be("corr-42");
        msg.Status.Should().Be(OutboxMessageStatus.Pending);
        msg.RetryCount.Should().Be(0);
        msg.ProcessedAt.Should().BeNull();
        msg.LockToken.Should().BeNull();
        msg.LockExpiry.Should().BeNull();
    }

    [Fact]
    public void MarkAsProcessing_SetsStatusAndLock()
    {
        var msg = CreateMessage();

        msg.MarkAsProcessing("lock-token-1", TimeSpan.FromMinutes(5));

        msg.Status.Should().Be(OutboxMessageStatus.Processing);
        msg.LockToken.Should().Be("lock-token-1");
        msg.LockExpiry.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(5), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkAsPublished_SetsStatusAndClearsLock()
    {
        var msg = CreateMessage();
        msg.MarkAsProcessing("tok", TimeSpan.FromMinutes(1));

        msg.MarkAsPublished();

        msg.Status.Should().Be(OutboxMessageStatus.Published);
        msg.ProcessedAt.Should().NotBeNull();
        msg.LockToken.Should().BeNull();
        msg.LockExpiry.Should().BeNull();
    }

    [Fact]
    public void MarkAsFailed_IncrementsRetry_RemainsRetrievable_WhenBelowMaxRetries()
    {
        var msg = CreateMessage();

        msg.MarkAsFailed("first failure", maxRetries: 3);

        msg.RetryCount.Should().Be(1);
        msg.LastError.Should().Be("first failure");
        msg.Status.Should().Be(OutboxMessageStatus.Pending);
        msg.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public void MarkAsFailed_SetsFailed_WhenMaxRetriesReached()
    {
        var msg = CreateMessage();

        msg.MarkAsFailed("err-1", maxRetries: 2);
        msg.MarkAsFailed("err-2", maxRetries: 2);

        msg.RetryCount.Should().Be(2);
        msg.Status.Should().Be(OutboxMessageStatus.Failed);
        msg.ProcessedAt.Should().NotBeNull();
        msg.LastError.Should().Be("err-2");
    }

    [Fact]
    public void ReleaseLock_ResetsStatusToPending_WhenProcessing()
    {
        var msg = CreateMessage();
        msg.MarkAsProcessing("tok", TimeSpan.FromMinutes(1));

        msg.ReleaseLock();

        msg.Status.Should().Be(OutboxMessageStatus.Pending);
        msg.LockToken.Should().BeNull();
        msg.LockExpiry.Should().BeNull();
    }

    [Fact]
    public void ReleaseLock_DoesNotChangeStatus_WhenNotProcessing()
    {
        var msg = CreateMessage();
        msg.MarkAsFailed("err", maxRetries: 1);

        msg.ReleaseLock();

        msg.Status.Should().Be(OutboxMessageStatus.Failed);
        msg.LockToken.Should().BeNull();
    }

    [Fact]
    public void IsLockExpired_ReturnsFalse_WhenNoLock()
    {
        var msg = CreateMessage();
        msg.IsLockExpired().Should().BeFalse();
    }

    [Fact]
    public void IsLockExpired_ReturnsFalse_WhenLockStillValid()
    {
        var msg = CreateMessage();
        msg.MarkAsProcessing("tok", TimeSpan.FromMinutes(10));

        msg.IsLockExpired().Should().BeFalse();
    }

    [Fact]
    public void CanBeProcessed_ReturnsTrue_WhenPending()
    {
        var msg = CreateMessage();
        msg.CanBeProcessed().Should().BeTrue();
    }

    [Fact]
    public void CanBeProcessed_ReturnsFalse_WhenProcessingWithValidLock()
    {
        var msg = CreateMessage();
        msg.MarkAsProcessing("tok", TimeSpan.FromMinutes(10));

        msg.CanBeProcessed().Should().BeFalse();
    }

    [Fact]
    public void CanBeProcessed_ReturnsFalse_WhenPublished()
    {
        var msg = CreateMessage();
        msg.MarkAsPublished();

        msg.CanBeProcessed().Should().BeFalse();
    }

    [Fact]
    public void FullLifecycle_PendingToProcessingToPublished()
    {
        var msg = CreateMessage();

        msg.Status.Should().Be(OutboxMessageStatus.Pending);

        msg.MarkAsProcessing("tok", TimeSpan.FromMinutes(5));
        msg.Status.Should().Be(OutboxMessageStatus.Processing);

        msg.MarkAsPublished();
        msg.Status.Should().Be(OutboxMessageStatus.Published);
        msg.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public void FullLifecycle_RetryThenEventualFailure()
    {
        var msg = CreateMessage();
        const int maxRetries = 3;

        for (int i = 1; i <= maxRetries; i++)
        {
            msg.MarkAsFailed($"attempt {i}", maxRetries);
        }

        msg.RetryCount.Should().Be(maxRetries);
        msg.Status.Should().Be(OutboxMessageStatus.Failed);
        msg.LastError.Should().Be($"attempt {maxRetries}");
    }
}
