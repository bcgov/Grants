using FluentAssertions;
using Grants.ApplicantPortal.API.UseCases;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Grants.ApplicantPortal.API.UnitTests.UseCases;

public class ProfileCacheInvalidationServiceTests
{
    private readonly ProfileCacheInvalidationService _sut;
    private readonly IDistributedCache _cache;
    private readonly ProfileCacheOptions _cacheOptions;

    public ProfileCacheInvalidationServiceTests()
    {
        _cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        _cacheOptions = new ProfileCacheOptions
        {
            CacheKeyPrefix = "profile:",
            CacheExpiryMinutes = 60,
            SlidingExpiryMinutes = 15
        };

        _sut = new ProfileCacheInvalidationService(
            _cache,
            Options.Create(_cacheOptions),
            NullLogger<ProfileCacheInvalidationService>.Instance);
    }

    [Fact]
    public async Task InvalidateProfileDataCacheAsync_RemovesEntry()
    {
        var profileId = Guid.NewGuid();
        var cacheKey = $"profile:{profileId}:UNITY:PROV1:CONTACTS";

        await _cache.SetStringAsync(cacheKey, "some-data");

        await _sut.InvalidateProfileDataCacheAsync(profileId, "UNITY", "PROV1", "CONTACTS", CancellationToken.None);

        var result = await _cache.GetStringAsync(cacheKey);
        result.Should().BeNull();
    }

    [Fact]
    public async Task InvalidateProfileDataCacheAsync_DoesNotThrow_WhenKeyDoesNotExist()
    {
        var profileId = Guid.NewGuid();

        var act = () => _sut.InvalidateProfileDataCacheAsync(profileId, "UNITY", "PROV1", "CONTACTS", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InvalidateProfileDataCacheAsync_DoesNotAffectOtherKeys()
    {
        var profileId = Guid.NewGuid();
        var targetKey = $"profile:{profileId}:UNITY:PROV1:CONTACTS";
        var otherKey = $"profile:{profileId}:UNITY:PROV1:ADDRESSES";

        await _cache.SetStringAsync(targetKey, "contacts-data");
        await _cache.SetStringAsync(otherKey, "addresses-data");

        await _sut.InvalidateProfileDataCacheAsync(profileId, "UNITY", "PROV1", "CONTACTS", CancellationToken.None);

        (await _cache.GetStringAsync(targetKey)).Should().BeNull();
        (await _cache.GetStringAsync(otherKey)).Should().Be("addresses-data");
    }
}
