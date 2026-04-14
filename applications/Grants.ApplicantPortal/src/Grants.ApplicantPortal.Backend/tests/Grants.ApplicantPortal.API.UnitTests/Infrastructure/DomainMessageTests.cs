using FluentAssertions;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Abstractions;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Messages;

namespace Grants.ApplicantPortal.API.UnitTests.Infrastructure;

public class DomainMessageTests
{
    [Fact]
    public void BaseMessage_SetsDefaults()
    {
        var msg = new ProfileUpdatedMessage(Guid.NewGuid(), "UNITY", "PROV1", "KEY1");

        msg.MessageId.Should().NotBeEmpty();
        msg.MessageType.Should().Be(nameof(ProfileUpdatedMessage));
        msg.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        msg.PluginId.Should().Be("UNITY");
    }

    [Fact]
    public void ProfileUpdatedMessage_SetsProperties()
    {
        var profileId = Guid.NewGuid();
        var msg = new ProfileUpdatedMessage(profileId, "DEMO", "PROV1", "ORGINFO", "corr-1");

        msg.ProfileId.Should().Be(profileId);
        msg.PluginId.Should().Be("DEMO");
        msg.Provider.Should().Be("PROV1");
        msg.Key.Should().Be("ORGINFO");
        msg.CorrelationId.Should().Be("corr-1");
    }

    [Fact]
    public void ContactCreatedMessage_SetsProperties()
    {
        var contactId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var msg = new ContactCreatedMessage(contactId, profileId, "UNITY", "John Doe", "PRIMARY");

        msg.ContactId.Should().Be(contactId);
        msg.ProfileId.Should().Be(profileId);
        msg.ContactName.Should().Be("John Doe");
        msg.ContactType.Should().Be("PRIMARY");
    }

    [Fact]
    public void AddressUpdatedMessage_SetsProperties()
    {
        var addressId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var msg = new AddressUpdatedMessage(addressId, profileId, "UNITY", "MAILING", true);

        msg.AddressId.Should().Be(addressId);
        msg.IsPrimary.Should().BeTrue();
        msg.AddressType.Should().Be("MAILING");
    }

    [Fact]
    public void OrganizationUpdatedMessage_SetsProperties()
    {
        var orgId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var msg = new OrganizationUpdatedMessage(orgId, profileId, "DEMO", "Acme Corp", "NON_PROFIT");

        msg.OrganizationId.Should().Be(orgId);
        msg.OrganizationName.Should().Be("Acme Corp");
        msg.OrganizationType.Should().Be("NON_PROFIT");
    }

    [Fact]
    public void SystemEventMessage_SetsPluginIdToSystem()
    {
        var msg = new SystemEventMessage("CLEANUP_COMPLETED", "Old messages cleaned up");

        msg.PluginId.Should().Be("SYSTEM");
        msg.EventType.Should().Be("CLEANUP_COMPLETED");
        msg.EventDescription.Should().Be("Old messages cleaned up");
    }

    [Fact]
    public void SystemEventMessage_CanIncludeEventData()
    {
        var data = new { Count = 42, Duration = "1m" };
        var msg = new SystemEventMessage("JOB_FINISHED", "done", data);

        msg.EventData.Should().NotBeNull();
    }

    [Fact]
    public void MessageAcknowledgment_SetsProperties()
    {
        var originalId = Guid.NewGuid();
        var msg = new MessageAcknowledgment(originalId, "FAILED", "UNITY", "timeout");

        msg.OriginalMessageId.Should().Be(originalId);
        msg.Status.Should().Be("FAILED");
        msg.PluginId.Should().Be("UNITY");
        msg.Details.Should().Be("timeout");
        msg.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void PluginDataMessage_SetsProperties()
    {
        var payload = new { Id = 1, Name = "Test" };
        var msg = new PluginDataMessage("DEMO", "PROFILE_UPDATE", payload, "corr-5");

        msg.PluginId.Should().Be("DEMO");
        msg.DataType.Should().Be("PROFILE_UPDATE");
        msg.Data.Should().Be(payload);
        msg.CorrelationId.Should().Be("corr-5");
    }

    [Fact]
    public void EachMessage_GetsUniqueId()
    {
        var msg1 = new SystemEventMessage("A", "a");
        var msg2 = new SystemEventMessage("B", "b");

        msg1.MessageId.Should().NotBe(msg2.MessageId);
    }
}
