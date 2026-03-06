using FluentAssertions;
using Grants.ApplicantPortal.API.Core.Plugins;
using NSubstitute;

namespace Grants.ApplicantPortal.API.UnitTests.Core;

public class PluginCommandMetadataRegistryTests
{
    private readonly IPluginCommandMetadataProvider _unityProvider;
    private readonly IPluginCommandMetadataProvider _demoProvider;
    private readonly PluginCommandMetadataRegistry _sut;

    public PluginCommandMetadataRegistryTests()
    {
        _unityProvider = Substitute.For<IPluginCommandMetadataProvider>();
        _unityProvider.PluginId.Returns("UNITY");

        _demoProvider = Substitute.For<IPluginCommandMetadataProvider>();
        _demoProvider.PluginId.Returns("DEMO");

        _sut = new PluginCommandMetadataRegistry(new[] { _unityProvider, _demoProvider });
    }

    [Fact]
    public void GetCacheSegment_DelegatesToCorrectProvider()
    {
        _unityProvider.GetCacheSegment("CONTACTS").Returns("contacts");

        var result = _sut.GetCacheSegment("UNITY", "CONTACTS");

        result.Should().Be("contacts");
    }

    [Fact]
    public void GetCacheSegment_ReturnsNull_ForUnknownPlugin()
    {
        var result = _sut.GetCacheSegment("UNKNOWN", "CONTACTS");

        result.Should().BeNull();
    }

    [Fact]
    public void GetCacheSegment_IsCaseInsensitive()
    {
        _unityProvider.GetCacheSegment("ADDRESSES").Returns("addresses");

        var result = _sut.GetCacheSegment("unity", "ADDRESSES");

        result.Should().Be("addresses");
    }

    [Fact]
    public void GetFriendlyActionName_DelegatesToCorrectProvider()
    {
        _demoProvider.GetFriendlyActionName("CREATE_CONTACT").Returns("contact creation");

        var result = _sut.GetFriendlyActionName("DEMO", "CREATE_CONTACT");

        result.Should().Be("contact creation");
    }

    [Fact]
    public void GetFriendlyActionName_ReturnsFallback_ForUnknownPlugin()
    {
        var result = _sut.GetFriendlyActionName("UNKNOWN", "CREATE_CONTACT");

        result.Should().Be("change");
    }

    [Fact]
    public void ParsePayload_DelegatesToCorrectProvider()
    {
        var expected = new CommandPayloadMetadata("CONTACTS", Guid.NewGuid(), "PROV1", "entity-1");
        _unityProvider.ParsePayload("{\"test\":1}").Returns(expected);

        var result = _sut.ParsePayload("UNITY", "{\"test\":1}");

        result.Should().Be(expected);
    }

    [Fact]
    public void ParsePayload_ReturnsNull_ForUnknownPlugin()
    {
        var result = _sut.ParsePayload("UNKNOWN", "{}");

        result.Should().BeNull();
    }

    [Fact]
    public void ParsePayload_ReturnsNull_WhenProviderReturnsNull()
    {
        _demoProvider.ParsePayload(Arg.Any<string>()).Returns((CommandPayloadMetadata?)null);

        var result = _sut.ParsePayload("DEMO", "{}");

        result.Should().BeNull();
    }

    [Fact]
    public void Constructor_HandlesEmptyProviderList()
    {
        var emptyRegistry = new PluginCommandMetadataRegistry(Enumerable.Empty<IPluginCommandMetadataProvider>());

        emptyRegistry.GetCacheSegment("UNITY", "CONTACTS").Should().BeNull();
        emptyRegistry.GetFriendlyActionName("UNITY", "CONTACTS").Should().Be("change");
        emptyRegistry.ParsePayload("UNITY", "{}").Should().BeNull();
    }
}
