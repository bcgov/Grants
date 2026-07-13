using FluentAssertions;
using Grants.ApplicantPortal.API.Plugins.Demo;
using Grants.ApplicantPortal.API.UseCases;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Grants.ApplicantPortal.API.UnitTests.Plugins;

public class DemoPluginTests
{
    private readonly DemoPlugin _sut;

    public DemoPluginTests()
    {
        var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var cacheOptions = Options.Create(new ProfileCacheOptions
        {
            CacheKeyPrefix = "profile:",
            CacheExpiryMinutes = 60,
            SlidingExpiryMinutes = 15
        });

        _sut = new DemoPlugin(NullLogger<DemoPlugin>.Instance, cache, cacheOptions);
    }

    [Fact]
    public async Task GetProvidersAsync_ReturnsExpectedProviders_WithDisplayNameAndDefaultFromAddress()
    {
        var providers = await _sut.GetProvidersAsync(Guid.NewGuid(), "subject");

        providers.Should().HaveCount(2);

        var program1 = providers.Single(p => p.Id == "PROGRAM1");
        program1.Name.Should().Be("PROGRAM1");
        program1.DisplayName.Should().Be("Program One");
        program1.DefaultFromAddress.Should().Be("NoReply@gov.bc.ca");

        var program2 = providers.Single(p => p.Id == "PROGRAM2");
        program2.Name.Should().Be("PROGRAM2");
        program2.DisplayName.Should().Be("Program Two");
        program2.DefaultFromAddress.Should().Be("NoReply@gov.bc.ca");
    }
}
