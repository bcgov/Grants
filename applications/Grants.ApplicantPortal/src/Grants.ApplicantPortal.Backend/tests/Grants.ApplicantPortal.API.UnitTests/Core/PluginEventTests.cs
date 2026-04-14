using FluentAssertions;
using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.UnitTests.Core;

public class PluginEventTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var profileId = Guid.NewGuid();
        var originalMsgId = Guid.NewGuid();

        var evt = new PluginEvent(
            profileId,
            "UNITY",
            "PROV1",
            "CONTACTS",
            "entity-123",
            PluginEventSeverity.Error,
            PluginEventSource.OutboxFailure,
            "Something went wrong",
            "Stack trace here",
            originalMsgId,
            "corr-42");

        evt.EventId.Should().NotBeEmpty();
        evt.ProfileId.Should().Be(profileId);
        evt.PluginId.Should().Be("UNITY");
        evt.Provider.Should().Be("PROV1");
        evt.DataType.Should().Be("CONTACTS");
        evt.EntityId.Should().Be("entity-123");
        evt.Severity.Should().Be(PluginEventSeverity.Error);
        evt.Source.Should().Be(PluginEventSource.OutboxFailure);
        evt.UserMessage.Should().Be("Something went wrong");
        evt.TechnicalDetails.Should().Be("Stack trace here");
        evt.OriginalMessageId.Should().Be(originalMsgId);
        evt.CorrelationId.Should().Be("corr-42");
        evt.IsAcknowledged.Should().BeFalse();
        evt.AcknowledgedAt.Should().BeNull();
        evt.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Constructor_AllowsNullOptionalFields()
    {
        var evt = new PluginEvent(
            Guid.NewGuid(),
            "DEMO",
            "PROV1",
            "ADDRESSES",
            entityId: null,
            PluginEventSeverity.Info,
            PluginEventSource.System,
            "Info event");

        evt.EntityId.Should().BeNull();
        evt.TechnicalDetails.Should().BeNull();
        evt.OriginalMessageId.Should().BeNull();
        evt.CorrelationId.Should().BeNull();
    }

    [Fact]
    public void Acknowledge_SetsFlag_AndTimestamp()
    {
        var evt = new PluginEvent(
            Guid.NewGuid(),
            "UNITY",
            "PROV1",
            "CONTACTS",
            null,
            PluginEventSeverity.Warning,
            PluginEventSource.InboxRejection,
            "Rejected");

        evt.Acknowledge();

        evt.IsAcknowledged.Should().BeTrue();
        evt.AcknowledgedAt.Should().NotBeNull();
        evt.AcknowledgedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Acknowledge_CalledTwice_DoesNotThrow()
    {
        var evt = new PluginEvent(
            Guid.NewGuid(),
            "DEMO",
            "PROV1",
            "ORG",
            null,
            PluginEventSeverity.Info,
            PluginEventSource.Plugin,
            "test");

        evt.Acknowledge();
        var firstAckTime = evt.AcknowledgedAt;

        evt.Acknowledge();

        evt.IsAcknowledged.Should().BeTrue();
        // Second call updates the timestamp
        evt.AcknowledgedAt.Should().BeOnOrAfter(firstAckTime!.Value);
    }

    [Fact]
    public void EachEvent_GetsUniqueId()
    {
        var evt1 = new PluginEvent(Guid.NewGuid(), "A", "P", "D", null, PluginEventSeverity.Info, PluginEventSource.System, "m1");
        var evt2 = new PluginEvent(Guid.NewGuid(), "A", "P", "D", null, PluginEventSeverity.Info, PluginEventSource.System, "m2");

        evt1.EventId.Should().NotBe(evt2.EventId);
    }
}
