using Ardalis.Result;
using FluentAssertions;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Abstractions;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Handlers;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Messages;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Grants.ApplicantPortal.API.UnitTests.Infrastructure;

public class PluginMessageRouterTests
{
    private readonly IPluginMessageHandler _unityHandler;
    private readonly IPluginMessageHandler _demoHandler;
    private readonly PluginMessageRouter _sut;

    public PluginMessageRouterTests()
    {
        _unityHandler = Substitute.For<IPluginMessageHandler>();
        _unityHandler.PluginId.Returns("UNITY");

        _demoHandler = Substitute.For<IPluginMessageHandler>();
        _demoHandler.PluginId.Returns("DEMO");

        _sut = new PluginMessageRouter(
            new[] { _unityHandler, _demoHandler },
            NullLogger<PluginMessageRouter>.Instance);
    }

    [Fact]
    public void GetPluginHandler_ReturnsCorrectHandler()
    {
        _sut.GetPluginHandler("UNITY").Should().Be(_unityHandler);
        _sut.GetPluginHandler("DEMO").Should().Be(_demoHandler);
    }

    [Fact]
    public void GetPluginHandler_ReturnsNull_ForUnknownPlugin()
    {
        _sut.GetPluginHandler("UNKNOWN").Should().BeNull();
    }

    [Fact]
    public void GetPluginHandler_IsCaseInsensitive()
    {
        _sut.GetPluginHandler("unity").Should().Be(_unityHandler);
    }

    [Fact]
    public void GetAllPluginHandlers_ReturnsAllRegistered()
    {
        _sut.GetAllPluginHandlers().Should().HaveCount(2);
    }

    [Fact]
    public async Task RouteToPluginAsync_RoutesAcknowledgment_ToCorrectPlugin()
    {
        var ack = new MessageAcknowledgment(Guid.NewGuid(), "SUCCESS", "UNITY");
        var ctx = new MessageContext(ack);

        _unityHandler.HandleAcknowledgmentAsync(ack, ctx).Returns(Result.Success());

        var result = await _sut.RouteToPluginAsync(ack, ctx);

        result.IsSuccess.Should().BeTrue();
        await _unityHandler.Received(1).HandleAcknowledgmentAsync(ack, ctx);
        await _demoHandler.DidNotReceive().HandleAcknowledgmentAsync(Arg.Any<MessageAcknowledgment>(), Arg.Any<MessageContext>());
    }

    [Fact]
    public async Task RouteToPluginAsync_ReturnsError_WhenNoHandlerForAcknowledgment()
    {
        var ack = new MessageAcknowledgment(Guid.NewGuid(), "SUCCESS", "UNKNOWN");
        var ctx = new MessageContext(ack);

        var result = await _sut.RouteToPluginAsync(ack, ctx);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task RouteToPluginAsync_RoutesDataMessage_ToCorrectPlugin()
    {
        var data = new PluginDataMessage("DEMO", "PROFILE_UPDATE", new { Id = 1 });
        var ctx = new MessageContext(data);

        _demoHandler.HandleIncomingDataAsync(data, ctx).Returns(Result.Success());

        var result = await _sut.RouteToPluginAsync(data, ctx);

        result.IsSuccess.Should().BeTrue();
        await _demoHandler.Received(1).HandleIncomingDataAsync(data, ctx);
    }

    [Fact]
    public async Task RouteToPluginAsync_ReturnsError_WhenNoHandlerForDataMessage()
    {
        var data = new PluginDataMessage("UNKNOWN", "TEST", new { });
        var ctx = new MessageContext(data);

        var result = await _sut.RouteToPluginAsync(data, ctx);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task RouteToPluginAsync_ReturnsError_WhenHandlerThrows()
    {
        var ack = new MessageAcknowledgment(Guid.NewGuid(), "SUCCESS", "UNITY");
        var ctx = new MessageContext(ack);

        _unityHandler.HandleAcknowledgmentAsync(ack, ctx)
            .ThrowsAsync(new InvalidOperationException("handler exploded"));

        var result = await _sut.RouteToPluginAsync(ack, ctx);

        result.IsSuccess.Should().BeFalse();
    }
}
