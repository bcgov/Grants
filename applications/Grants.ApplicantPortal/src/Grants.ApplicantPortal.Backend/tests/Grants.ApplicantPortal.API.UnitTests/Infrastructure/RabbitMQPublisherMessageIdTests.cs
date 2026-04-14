using FluentAssertions;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.RabbitMQ;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using RabbitMQ.Client;

namespace Grants.ApplicantPortal.API.UnitTests.Infrastructure;

/// <summary>
/// Tests that <see cref="RabbitMQPublisher"/> sets the AMQP MessageId property
/// from the caller-supplied messageId (outbox MessageId) rather than generating
/// a new GUID. This is critical for the ack correlation loop — external systems
/// echo the AMQP MessageId back as <c>originalMessageId</c>.
/// </summary>
public class RabbitMQPublisherMessageIdTests
{
    private readonly IModel _channel;
    private readonly RabbitMQPublisher _sut;
    private readonly BasicProperties _capturedProperties;

    public RabbitMQPublisherMessageIdTests()
    {
        _capturedProperties = new BasicProperties();

        _channel = Substitute.For<IModel>();
        _channel.IsOpen.Returns(true);
        _channel.CreateBasicProperties().Returns(_capturedProperties);
        _channel.WaitForConfirms(Arg.Any<TimeSpan>()).Returns(true);

        var config = new RabbitMQConfiguration
        {
            HostName = "localhost",
            Port = 5672,
            UserName = "guest",
            Password = "guest",
            DefaultExchange = "test-exchange",
            MaxMessageSize = 1_048_576,
            PublisherConfirmTimeout = TimeSpan.FromSeconds(5)
        };

        // Constructor no longer connects — inject the mock channel via reflection
        _sut = CreatePublisherWithChannel(_channel, config);
    }

    [Fact]
    public async Task PublishAsync_UsesSuppliedMessageId_ForAmqpProperty()
    {
        var outboxMessageId = Guid.NewGuid();

        await _sut.PublishAsync("TestType", "{}", routingKey: "test.key", correlationId: null,
            messageId: outboxMessageId);

        _capturedProperties.MessageId.Should().Be(outboxMessageId.ToString());
    }

    [Fact]
    public async Task PublishAsync_GeneratesNewGuid_WhenMessageIdIsNull()
    {
        await _sut.PublishAsync("TestType", "{}", routingKey: "test.key", correlationId: null,
            messageId: null);

        Guid.TryParse(_capturedProperties.MessageId, out _).Should().BeTrue("a valid GUID should be generated");
    }

    [Fact]
    public async Task PublishAsync_SetsCorrelationId_WhenProvided()
    {
        await _sut.PublishAsync("TestType", "{}", routingKey: "test.key",
            correlationId: "profile-abc", messageId: Guid.NewGuid());

        _capturedProperties.CorrelationId.Should().Be("profile-abc");
    }

    /// <summary>
    /// Creates a <see cref="RabbitMQPublisher"/> with an injected channel.
    /// The constructor no longer connects to RabbitMQ, so we can construct
    /// normally and inject the mock channel via reflection.
    /// </summary>
    private static RabbitMQPublisher CreatePublisherWithChannel(IModel channel, RabbitMQConfiguration config)
    {
        var publisher = new RabbitMQPublisher(config, NullLogger<RabbitMQPublisher>.Instance);

        var type = typeof(RabbitMQPublisher);
        type.GetField("_channel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .SetValue(publisher, channel);

        return publisher;
    }

    private class BasicProperties : IBasicProperties
    {
        public string? AppId { get; set; }
        public string? ClusterId { get; set; }
        public string? ContentEncoding { get; set; }
        public string? ContentType { get; set; }
        public string? CorrelationId { get; set; }
        public byte DeliveryMode { get; set; }
        public string? Expiration { get; set; }
        public IDictionary<string, object>? Headers { get; set; }
        public string? MessageId { get; set; }
        public bool Persistent { get; set; }
        public byte Priority { get; set; }
        public string? ReplyTo { get; set; }
        public PublicationAddress? ReplyToAddress { get; set; }
        public AmqpTimestamp Timestamp { get; set; }
        public string? Type { get; set; }
        public string? UserId { get; set; }

        public ushort ProtocolClassId => 60;
        public string ProtocolClassName => "basic";

        public void ClearAppId() => AppId = null;
        public void ClearClusterId() => ClusterId = null;
        public void ClearContentEncoding() => ContentEncoding = null;
        public void ClearContentType() => ContentType = null;
        public void ClearCorrelationId() => CorrelationId = null;
        public void ClearDeliveryMode() => DeliveryMode = 0;
        public void ClearExpiration() => Expiration = null;
        public void ClearHeaders() => Headers = null;
        public void ClearMessageId() => MessageId = null;
        public void ClearPriority() => Priority = 0;
        public void ClearReplyTo() => ReplyTo = null;
        public void ClearTimestamp() => Timestamp = default;
        public void ClearType() => Type = null;
        public void ClearUserId() => UserId = null;

        public bool IsAppIdPresent() => AppId != null;
        public bool IsClusterIdPresent() => ClusterId != null;
        public bool IsContentEncodingPresent() => ContentEncoding != null;
        public bool IsContentTypePresent() => ContentType != null;
        public bool IsCorrelationIdPresent() => CorrelationId != null;
        public bool IsDeliveryModePresent() => DeliveryMode != 0;
        public bool IsExpirationPresent() => Expiration != null;
        public bool IsHeadersPresent() => Headers != null;
        public bool IsMessageIdPresent() => MessageId != null;
        public bool IsPriorityPresent() => Priority != 0;
        public bool IsReplyToPresent() => ReplyTo != null;
        public bool IsTimestampPresent() => Timestamp.UnixTime != 0;
        public bool IsTypePresent() => Type != null;
        public bool IsUserIdPresent() => UserId != null;
    }
}
