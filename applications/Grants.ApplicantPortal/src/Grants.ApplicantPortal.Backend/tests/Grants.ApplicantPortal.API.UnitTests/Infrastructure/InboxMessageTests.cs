using FluentAssertions;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Inbox;

namespace Grants.ApplicantPortal.API.UnitTests.Infrastructure;

public class InboxMessageTests
{
    private InboxMessage CreateMessage(string type = "TestMessage", string payload = "{}")
        => new(Guid.NewGuid(), type, payload, "corr-1");

    [Fact]
    public void Constructor_SetsInitialState()
    {
        var messageId = Guid.NewGuid();
        var msg = new InboxMessage(messageId, "InboxType", "{\"b\":2}", "corr-99");

        msg.MessageId.Should().Be(messageId);
        msg.MessageType.Should().Be("InboxType");
        msg.Payload.Should().Be("{\"b\":2}");
        msg.CorrelationId.Should().Be("corr-99");
        msg.Status.Should().Be(InboxMessageStatus.Pending);
        msg.RetryCount.Should().Be(0);
        msg.ProcessedAt.Should().BeNull();
        msg.LockToken.Should().BeNull();
        msg.LockExpiry.Should().BeNull();
    }

    [Fact]
    public void MarkAsProcessing_SetsStatusAndLock()
    {
        var msg = CreateMessage();

        msg.MarkAsProcessing("lock-abc", TimeSpan.FromMinutes(3));

        msg.Status.Should().Be(InboxMessageStatus.Processing);
        msg.LockToken.Should().Be("lock-abc");
        msg.LockExpiry.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(3), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkAsProcessed_SetsStatusAndClearsLock()
    {
        var msg = CreateMessage();
        msg.MarkAsProcessing("tok", TimeSpan.FromMinutes(1));

        msg.MarkAsProcessed();

        msg.Status.Should().Be(InboxMessageStatus.Processed);
        msg.ProcessedAt.Should().NotBeNull();
        msg.LockToken.Should().BeNull();
        msg.LockExpiry.Should().BeNull();
    }

    [Fact]
    public void MarkAsFailed_IncrementsRetry_RemainsRetrievable_WhenBelowMaxRetries()
    {
        var msg = CreateMessage();

        msg.MarkAsFailed("fail 1", maxRetries: 3);

        msg.RetryCount.Should().Be(1);
        msg.LastError.Should().Be("fail 1");
        msg.Status.Should().Be(InboxMessageStatus.Pending);
        msg.ProcessedAt.Should().BeNull();
    }

    [Fact]
    public void MarkAsFailed_SetsFailed_WhenMaxRetriesReached()
    {
        var msg = CreateMessage();

        msg.MarkAsFailed("err-1", maxRetries: 2);
        msg.MarkAsFailed("err-2", maxRetries: 2);

        msg.RetryCount.Should().Be(2);
        msg.Status.Should().Be(InboxMessageStatus.Failed);
        msg.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsDuplicate_SetsStatusAndClearsLock()
    {
        var msg = CreateMessage();
        msg.MarkAsProcessing("tok", TimeSpan.FromMinutes(1));

        msg.MarkAsDuplicate();

        msg.Status.Should().Be(InboxMessageStatus.Duplicate);
        msg.ProcessedAt.Should().NotBeNull();
        msg.LockToken.Should().BeNull();
    }

    [Fact]
    public void ReleaseLock_ResetsStatusToPending_WhenProcessing()
    {
        var msg = CreateMessage();
        msg.MarkAsProcessing("tok", TimeSpan.FromMinutes(1));

        msg.ReleaseLock();

        msg.Status.Should().Be(InboxMessageStatus.Pending);
        msg.LockToken.Should().BeNull();
        msg.LockExpiry.Should().BeNull();
    }

    [Fact]
    public void ReleaseLock_DoesNotChangeStatus_WhenNotProcessing()
    {
        var msg = CreateMessage();
        msg.MarkAsFailed("err", maxRetries: 1);

        msg.ReleaseLock();

        msg.Status.Should().Be(InboxMessageStatus.Failed);
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
    public void CanBeProcessed_ReturnsFalse_WhenAlreadyProcessed()
    {
        var msg = CreateMessage();
        msg.MarkAsProcessed();

        msg.CanBeProcessed().Should().BeFalse();
    }

    [Fact]
    public void FullLifecycle_PendingToProcessingToProcessed()
    {
        var msg = CreateMessage();

        msg.Status.Should().Be(InboxMessageStatus.Pending);

        msg.MarkAsProcessing("tok", TimeSpan.FromMinutes(5));
        msg.Status.Should().Be(InboxMessageStatus.Processing);

        msg.MarkAsProcessed();
        msg.Status.Should().Be(InboxMessageStatus.Processed);
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
        msg.Status.Should().Be(InboxMessageStatus.Failed);
        msg.LastError.Should().Be($"attempt {maxRetries}");
    }
}
