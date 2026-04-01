using System.Text.Json;
using FluentAssertions;
using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.UseCases;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Grants.ApplicantPortal.API.UnitTests.UseCases;

public class PluginCacheServiceTests
{
    private readonly PluginCacheService _sut;
    private readonly IDistributedCache _cache;
    private readonly ProfileCacheOptions _cacheOptions;

    public PluginCacheServiceTests()
    {
        _cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        _cacheOptions = new ProfileCacheOptions
        {
            CacheKeyPrefix = "profile:",
            CacheExpiryMinutes = 60,
            SlidingExpiryMinutes = 15
        };

        _sut = new PluginCacheService(
            _cache,
            Options.Create(_cacheOptions),
            NullLogger<PluginCacheService>.Instance);
    }

    [Fact]
    public void BuildCacheKey_FollowsConvention()
    {
        var profileId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var key = _sut.BuildCacheKey(profileId, "UNITY", "contacts");

        key.Should().Be("profile:11111111-1111-1111-1111-111111111111:UNITY:contacts");
    }

    [Fact]
    public async Task GetOrFetchAsync_ReturnsCachedValue_OnHit()
    {
        var profileId = Guid.NewGuid();
        var expected = new TestData("cached-value");

        // Pre-populate cache
        var key = _sut.BuildCacheKey(profileId, "UNITY", "test");
        await _cache.SetStringAsync(key, JsonSerializer.Serialize(expected));

        var factoryCallCount = 0;
        var result = await _sut.GetOrFetchAsync<TestData>(
            profileId, "UNITY", "test",
            _ => { factoryCallCount++; return Task.FromResult(new TestData("factory-value")); });

        result.Name.Should().Be("cached-value");
        factoryCallCount.Should().Be(0);
    }

    [Fact]
    public async Task GetOrFetchAsync_CallsFactory_OnMiss()
    {
        var profileId = Guid.NewGuid();
        var factoryCallCount = 0;

        var result = await _sut.GetOrFetchAsync<TestData>(
            profileId, "UNITY", "test",
            _ => { factoryCallCount++; return Task.FromResult(new TestData("from-factory")); });

        result.Name.Should().Be("from-factory");
        factoryCallCount.Should().Be(1);
    }

    [Fact]
    public async Task GetOrFetchAsync_CachesFactoryResult_ByDefault()
    {
        var profileId = Guid.NewGuid();
        var callCount = 0;

        // First call — cache miss
        await _sut.GetOrFetchAsync<TestData>(
            profileId, "DEMO", "seg",
            _ => { callCount++; return Task.FromResult(new TestData("value")); });

        // Second call — should be a cache hit
        await _sut.GetOrFetchAsync<TestData>(
            profileId, "DEMO", "seg",
            _ => { callCount++; return Task.FromResult(new TestData("never")); });

        callCount.Should().Be(1);
    }

    [Fact]
    public async Task GetOrFetchAsync_SkipsCache_WhenShouldCacheReturnsFalse()
    {
        var profileId = Guid.NewGuid();
        var callCount = 0;

        // First call — factory returns empty, shouldCache says false
        await _sut.GetOrFetchAsync<TestData>(
            profileId, "UNITY", "seg",
            _ => { callCount++; return Task.FromResult(new TestData("")); },
            shouldCache: result => !string.IsNullOrEmpty(result.Name));

        // Second call — should still miss because first result wasn't cached
        await _sut.GetOrFetchAsync<TestData>(
            profileId, "UNITY", "seg",
            _ => { callCount++; return Task.FromResult(new TestData("real")); },
            shouldCache: result => !string.IsNullOrEmpty(result.Name));

        callCount.Should().Be(2);
    }

    [Fact]
    public async Task GetOrFetchAsync_RemovesCorruptCache_AndFallsThrough()
    {
        var profileId = Guid.NewGuid();
        var key = _sut.BuildCacheKey(profileId, "UNITY", "corrupt");

        // Put invalid JSON
        await _cache.SetStringAsync(key, "{{{{not json!!!!");

        var result = await _sut.GetOrFetchAsync<TestData>(
            profileId, "UNITY", "corrupt",
            _ => Task.FromResult(new TestData("recovered")));

        result.Name.Should().Be("recovered");

        // Corrupt entry should have been removed and replaced
        var cached = await _cache.GetStringAsync(key);
        cached.Should().NotBeNull();
        cached.Should().Contain("recovered");
    }

    [Fact]
    public async Task GetOrFetchAsync_StampsProfileData_CacheHit()
    {
        var profileId = Guid.NewGuid();
        var pd = new ProfileData(profileId, "UNITY", "PROV1", "KEY1", new { });

        var key = _sut.BuildCacheKey(profileId, "UNITY", "profile");
        await _cache.SetStringAsync(key, JsonSerializer.Serialize(pd));

        var result = await _sut.GetOrFetchAsync<ProfileData>(
            profileId, "UNITY", "profile",
            _ => Task.FromResult(pd));

        result.CacheStatus.Should().Be("HIT");
    }

    [Fact]
    public async Task GetOrFetchAsync_StampsProfileData_CacheMiss()
    {
        var profileId = Guid.NewGuid();
        var pd = new ProfileData(profileId, "UNITY", "PROV1", "KEY1", new { });

        var result = await _sut.GetOrFetchAsync<ProfileData>(
            profileId, "UNITY", "profile",
            _ => Task.FromResult(pd));

        result.CacheStatus.Should().Be("MISS");
    }

    [Fact]
    public async Task InvalidateAsync_RemovesCachedEntry()
    {
        var profileId = Guid.NewGuid();

        // Populate cache
        await _sut.GetOrFetchAsync<TestData>(
            profileId, "UNITY", "seg",
            _ => Task.FromResult(new TestData("cached")));

        // Invalidate
        await _sut.InvalidateAsync(profileId, "UNITY", "seg");

        // Next fetch should call factory again
        var callCount = 0;
        await _sut.GetOrFetchAsync<TestData>(
            profileId, "UNITY", "seg",
            _ => { callCount++; return Task.FromResult(new TestData("fresh")); });

        callCount.Should().Be(1);
    }

    [Fact]
    public async Task TryGetAsync_ReturnsNull_WhenNotCached()
    {
        var result = await _sut.TryGetAsync<TestData>(Guid.NewGuid(), "UNITY", "seg");

        result.Should().BeNull();
    }

    [Fact]
    public async Task TryGetAsync_ReturnsCachedValue_WhenPresent()
    {
        var profileId = Guid.NewGuid();

        await _sut.GetOrFetchAsync<TestData>(
            profileId, "DEMO", "seg",
            _ => Task.FromResult(new TestData("hello")));

        var result = await _sut.TryGetAsync<TestData>(profileId, "DEMO", "seg");

        result.Should().NotBeNull();
        result!.Name.Should().Be("hello");
    }

    [Fact]
    public async Task SetAsync_WritesValue_ThatCanBeRead()
    {
        var profileId = Guid.NewGuid();
        var data = new TestData("optimistic");

        await _sut.SetAsync(profileId, "UNITY", "seg", data);

        var result = await _sut.TryGetAsync<TestData>(profileId, "UNITY", "seg");
        result.Should().NotBeNull();
        result!.Name.Should().Be("optimistic");
    }

    [Fact]
    public async Task DifferentPlugins_HaveIndependentCaches()
    {
        var profileId = Guid.NewGuid();

        await _sut.SetAsync(profileId, "UNITY", "contacts", new TestData("unity-data"));
        await _sut.SetAsync(profileId, "DEMO", "contacts", new TestData("demo-data"));

        var unityResult = await _sut.TryGetAsync<TestData>(profileId, "UNITY", "contacts");
        var demoResult = await _sut.TryGetAsync<TestData>(profileId, "DEMO", "contacts");

        unityResult!.Name.Should().Be("unity-data");
        demoResult!.Name.Should().Be("demo-data");
    }

    [Fact]
    public async Task DifferentSegments_HaveIndependentCaches()
    {
        var profileId = Guid.NewGuid();

        await _sut.SetAsync(profileId, "UNITY", "contacts", new TestData("contacts-data"));
        await _sut.SetAsync(profileId, "UNITY", "addresses", new TestData("addresses-data"));

        var contactsResult = await _sut.TryGetAsync<TestData>(profileId, "UNITY", "contacts");
        var addressesResult = await _sut.TryGetAsync<TestData>(profileId, "UNITY", "addresses");

        contactsResult!.Name.Should().Be("contacts-data");
        addressesResult!.Name.Should().Be("addresses-data");
    }

    private record TestData(string Name);
}
