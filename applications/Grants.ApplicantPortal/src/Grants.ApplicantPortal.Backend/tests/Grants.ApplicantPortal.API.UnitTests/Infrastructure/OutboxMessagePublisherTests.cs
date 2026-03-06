using Ardalis.Result;
using FluentAssertions;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Abstractions;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Outbox;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Grants.ApplicantPortal.API.UnitTests.Infrastructure;

public class OutboxMessagePublisherTests
{
    private readonly IOutboxRepository _outboxRepo;
    private readonly OutboxMessagePublisher _sut;

    public OutboxMessagePublisherTests()
    {
        _outboxRepo = Substitute.For<IOutboxRepository>();
        _sut = new OutboxMessagePublisher(_outboxRepo, NullLogger<OutboxMessagePublisher>.Instance);
    }

    [Fact]
    public async Task PublishAsync_AddsMessageToOutbox()
    {
        _outboxRepo.AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var message = new TestMessage("UNITY", "corr-1");

        var result = await _sut.PublishAsync(message);

        result.IsSuccess.Should().BeTrue();
        await _outboxRepo.Received(1).AddAsync(
            Arg.Is<OutboxMessage>(m =>
                m.MessageId == message.MessageId &&
                m.MessageType == message.MessageType &&
                m.PluginId == "UNITY"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishAsync_ReturnsError_WhenRepositoryFails()
    {
        _outboxRepo.AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>())
            .Returns(Result.Error("DB down"));

        var result = await _sut.PublishAsync(new TestMessage("DEMO"));

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task PublishAsync_ReturnsError_WhenExceptionThrown()
    {
        _outboxRepo.AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await _sut.PublishAsync(new TestMessage("DEMO"));

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task PublishBatchAsync_ReturnsSuccess_ForEmptyBatch()
    {
        var result = await _sut.PublishBatchAsync(Enumerable.Empty<IMessage>());

        result.IsSuccess.Should().BeTrue();
        await _outboxRepo.DidNotReceive().AddBatchAsync(Arg.Any<IEnumerable<OutboxMessage>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishBatchAsync_AddsAllMessagesToOutbox()
    {
        _outboxRepo.AddBatchAsync(Arg.Any<IEnumerable<OutboxMessage>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var messages = new IMessage[]
        {
            new TestMessage("UNITY", "corr-1"),
            new TestMessage("DEMO", "corr-2")
        };

        var result = await _sut.PublishBatchAsync(messages);

        result.IsSuccess.Should().BeTrue();
        await _outboxRepo.Received(1).AddBatchAsync(Arg.Any<IEnumerable<OutboxMessage>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishBatchAsync_ReturnsError_WhenRepositoryFails()
    {
        _outboxRepo.AddBatchAsync(Arg.Any<IEnumerable<OutboxMessage>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Error("batch failed"));

        var messages = new IMessage[] { new TestMessage("UNITY") };

        var result = await _sut.PublishBatchAsync(messages);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task PublishBatchAsync_ReturnsError_WhenExceptionThrown()
    {
        _outboxRepo.AddBatchAsync(Arg.Any<IEnumerable<OutboxMessage>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("boom"));

        var messages = new IMessage[] { new TestMessage("DEMO") };

        var result = await _sut.PublishBatchAsync(messages);

        result.IsSuccess.Should().BeFalse();
    }

    /// <summary>
    /// Concrete test message for publisher tests.
    /// </summary>
    private record TestMessage : BaseMessage
    {
        public TestMessage(string? pluginId = null, string? correlationId = null)
            : base(correlationId, pluginId) { }

        public string SampleProperty { get; init; } = "test-value";
    }
}
