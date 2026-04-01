using FluentAssertions;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Abstractions;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Messages;

namespace Grants.ApplicantPortal.API.UnitTests.Infrastructure;

public class MessageContextTests
{
    [Fact]
    public void Constructor_SetsDefaults()
    {
        var message = new SystemEventMessage("TEST", "test event");
        var ctx = new MessageContext(message);

        ctx.Message.Should().Be(message);
        ctx.Properties.Should().BeEmpty();
        ctx.ProcessingStartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        ctx.CancellationToken.Should().Be(CancellationToken.None);
    }

    [Fact]
    public void Constructor_AcceptsProperties()
    {
        var message = new SystemEventMessage("TEST", "test event");
        var props = new Dictionary<string, object> { { "key1", "value1" } };

        var ctx = new MessageContext(message, props);

        ctx.Properties.Should().ContainKey("key1");
    }

    [Fact]
    public void GetProperty_ReturnsTypedValue_WhenPresent()
    {
        var message = new SystemEventMessage("TEST", "test event");
        var ctx = new MessageContext(message);
        ctx.Properties["count"] = 42;

        ctx.GetProperty<int>("count").Should().Be(42);
    }

    [Fact]
    public void GetProperty_ReturnsDefault_WhenKeyMissing()
    {
        var message = new SystemEventMessage("TEST", "test event");
        var ctx = new MessageContext(message);

        ctx.GetProperty<int>("missing").Should().Be(0);
        ctx.GetProperty<string>("missing").Should().BeNull();
    }

    [Fact]
    public void GetProperty_ReturnsDefault_WhenTypeMismatch()
    {
        var message = new SystemEventMessage("TEST", "test event");
        var ctx = new MessageContext(message);
        ctx.Properties["value"] = "not-an-int";

        ctx.GetProperty<int>("value").Should().Be(0);
    }

    [Fact]
    public void SetProperty_StoresValue()
    {
        var message = new SystemEventMessage("TEST", "test event");
        var ctx = new MessageContext(message);

        ctx.SetProperty("myKey", "myValue");

        ctx.Properties.Should().ContainKey("myKey");
        ctx.GetProperty<string>("myKey").Should().Be("myValue");
    }

    [Fact]
    public void SetProperty_DoesNotStore_WhenValueIsNull()
    {
        var message = new SystemEventMessage("TEST", "test event");
        var ctx = new MessageContext(message);

        ctx.SetProperty<string?>("myKey", null);

        ctx.Properties.Should().NotContainKey("myKey");
    }

    [Fact]
    public void CancellationToken_CanBeSet()
    {
        var cts = new CancellationTokenSource();
        var message = new SystemEventMessage("TEST", "test event");
        var ctx = new MessageContext(message) { CancellationToken = cts.Token };

        ctx.CancellationToken.Should().Be(cts.Token);
    }
}
