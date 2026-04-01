using FluentAssertions;
using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.UnitTests.Core;

public class PluginEventContextTests
{
    [Fact]
    public void Record_SetsAllProperties()
    {
        var profileId = Guid.NewGuid();
        var msgId = Guid.NewGuid();

        var ctx = new PluginEventContext(
            ProfileId: profileId,
            PluginId: "UNITY",
            Provider: "PROV1",
            DataType: "CONTACTS",
            EntityId: "ent-1",
            Severity: PluginEventSeverity.Error,
            Source: PluginEventSource.OutboxFailure,
            UserMessage: "Failed to sync contact",
            TechnicalDetails: "Connection timed out",
            OriginalMessageId: msgId,
            CorrelationId: "corr-99");

        ctx.ProfileId.Should().Be(profileId);
        ctx.PluginId.Should().Be("UNITY");
        ctx.Provider.Should().Be("PROV1");
        ctx.DataType.Should().Be("CONTACTS");
        ctx.EntityId.Should().Be("ent-1");
        ctx.Severity.Should().Be(PluginEventSeverity.Error);
        ctx.Source.Should().Be(PluginEventSource.OutboxFailure);
        ctx.UserMessage.Should().Be("Failed to sync contact");
        ctx.TechnicalDetails.Should().Be("Connection timed out");
        ctx.OriginalMessageId.Should().Be(msgId);
        ctx.CorrelationId.Should().Be("corr-99");
    }

    [Fact]
    public void Record_OptionalFieldsDefaultToNull()
    {
        var ctx = new PluginEventContext(
            ProfileId: Guid.NewGuid(),
            PluginId: "DEMO",
            Provider: "PROV1",
            DataType: "ADDRESSES",
            EntityId: null,
            Severity: PluginEventSeverity.Info,
            Source: PluginEventSource.System,
            UserMessage: "Info event");

        ctx.EntityId.Should().BeNull();
        ctx.TechnicalDetails.Should().BeNull();
        ctx.OriginalMessageId.Should().BeNull();
        ctx.CorrelationId.Should().BeNull();
    }

    [Fact]
    public void WithExpression_CanOverrideSeverity()
    {
        var original = new PluginEventContext(
            Guid.NewGuid(), "UNITY", "P", "D", null,
            PluginEventSeverity.Info, PluginEventSource.Plugin, "msg");

        var upgraded = original with { Severity = PluginEventSeverity.Error };

        upgraded.Severity.Should().Be(PluginEventSeverity.Error);
        upgraded.PluginId.Should().Be(original.PluginId);
        upgraded.UserMessage.Should().Be(original.UserMessage);
    }
}
