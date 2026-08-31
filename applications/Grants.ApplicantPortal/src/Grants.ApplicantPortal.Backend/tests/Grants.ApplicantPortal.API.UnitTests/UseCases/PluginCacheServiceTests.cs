using System.Text.Json;
using FluentAssertions;
using Grants.ApplicantPortal.API.Core.DTOs;
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

    /// <summary>
    /// Regression test for the cache-hit casing bug: <see cref="ProfileData.Data"/> is
    /// typed <c>object</c>. On a cache miss, the factory's real typed object flows straight
    /// to the HTTP response and is serialized camelCase by the app's own JSON options. On a
    /// cache hit, the previously cached JSON is deserialized into <see cref="ProfileData"/>,
    /// but the <c>Data</c> property becomes a <see cref="JsonElement"/> that retains whatever
    /// casing was baked into the stored JSON at write time — case-insensitive matching on read
    /// only resolves declared C# property names, it does not renormalize casing inside a
    /// JsonElement. If the cache write used default (PascalCase) casing, a cache-hit response
    /// would come back as <c>{ Schema: ..., Data: ... }</c> instead of <c>{ schema: ..., data: ... }</c>,
    /// even though the top-level <see cref="ProfileData"/> properties serialize camelCase via the
    /// app's HTTP JSON options. This test exercises a real cache round trip (via
    /// <see cref="MemoryDistributedCache"/>) and re-serializes the cache-HIT result the same way
    /// the HTTP layer would, proving the nested payload's casing is consistent between a
    /// cache-miss and a cache-hit response.
    /// </summary>
    [Fact]
    public async Task GetOrFetchAsync_CacheHitResult_SerializesNestedDataCamelCase_MatchingHttpLayer()
    {
        // Mirrors the app's own HTTP JSON serialization behaviour (FastEndpoints/System.Text.Json
        // camelCase — the same convention used throughout the codebase, e.g. DemoPlugin, Unity
        // plugin cache helpers, outbox/inbox messaging).
        var httpJsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var profileId = Guid.NewGuid();

        ProfileData Factory() => new(
            profileId,
            "DEMO",
            "PROGRAM1",
            "SUBMISSIONFORM",
            new SubmissionFormResponse(
                Schema: new { display = "form" },
                Data: new { data = new { organizationName = "Demo Society" } }));

        // First call — cache miss, factory's real typed object is returned directly.
        var missResult = await _sut.GetOrFetchAsync(
            profileId, "DEMO", "submissionform", _ => Task.FromResult(Factory()));
        missResult.CacheStatus.Should().Be("MISS");

        var missJson = JsonSerializer.Serialize(missResult, httpJsonOptions);
        missJson.Should().Contain("\"schema\"");
        missJson.Should().NotContain("\"Schema\"");

        // Second call — cache hit, Data comes back as a JsonElement sourced from the cached JSON.
        var hitResult = await _sut.GetOrFetchAsync<ProfileData>(
            profileId, "DEMO", "submissionform",
            _ => throw new InvalidOperationException("Factory should not be invoked on a cache hit"));
        hitResult.CacheStatus.Should().Be("HIT");

        var hitJson = JsonSerializer.Serialize(hitResult, httpJsonOptions);

        // This is the assertion that would have failed before the fix: without camelCase cache
        // writes, the nested JsonElement retains the PascalCase keys baked in at cache-write time,
        // producing "Schema"/"Data" here instead of "schema"/"data".
        hitJson.Should().Contain("\"schema\"");
        hitJson.Should().NotContain("\"Schema\"");
    }

    private record TestData(string Name);
}
