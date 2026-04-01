using FluentAssertions;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.BackgroundJobs;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Grants.ApplicantPortal.API.UnitTests.Infrastructure;

public class InMemoryDistributedLockTests
{
    private readonly InMemoryDistributedLock _sut;
    private readonly IDistributedCache _cache;

    public InMemoryDistributedLockTests()
    {
        _cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        _sut = new InMemoryDistributedLock(_cache, NullLogger<InMemoryDistributedLock>.Instance);
    }

    [Fact]
    public async Task AcquireLockAsync_Succeeds_WhenLockIsFree()
    {
        var result = await _sut.AcquireLockAsync("test-key", TimeSpan.FromMinutes(1));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task AcquireLockAsync_Fails_WhenLockAlreadyHeld()
    {
        await _sut.AcquireLockAsync("test-key", TimeSpan.FromMinutes(5));

        var secondResult = await _sut.AcquireLockAsync("test-key", TimeSpan.FromMinutes(5));

        secondResult.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task AcquireLockAsync_DifferentKeys_AreIndependent()
    {
        var result1 = await _sut.AcquireLockAsync("key-a", TimeSpan.FromMinutes(1));
        var result2 = await _sut.AcquireLockAsync("key-b", TimeSpan.FromMinutes(1));

        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ReleaseLockAsync_Succeeds_WithCorrectToken()
    {
        var acquired = await _sut.AcquireLockAsync("test-key", TimeSpan.FromMinutes(1));
        var token = acquired.Value;

        var result = await _sut.ReleaseLockAsync("test-key", token);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ReleaseLockAsync_Fails_WithWrongToken()
    {
        await _sut.AcquireLockAsync("test-key", TimeSpan.FromMinutes(1));

        var result = await _sut.ReleaseLockAsync("test-key", "wrong-token");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ReleaseLockAsync_Fails_WhenLockDoesNotExist()
    {
        var result = await _sut.ReleaseLockAsync("nonexistent-key", "any-token");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task AcquireLockAsync_Succeeds_AfterRelease()
    {
        var acquired = await _sut.AcquireLockAsync("test-key", TimeSpan.FromMinutes(1));
        await _sut.ReleaseLockAsync("test-key", acquired.Value);

        var reacquired = await _sut.AcquireLockAsync("test-key", TimeSpan.FromMinutes(1));

        reacquired.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RenewLockAsync_Succeeds_WithCorrectToken()
    {
        var acquired = await _sut.AcquireLockAsync("test-key", TimeSpan.FromMinutes(1));
        var token = acquired.Value;

        var result = await _sut.RenewLockAsync("test-key", token, TimeSpan.FromMinutes(10));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RenewLockAsync_Fails_WithWrongToken()
    {
        await _sut.AcquireLockAsync("test-key", TimeSpan.FromMinutes(1));

        var result = await _sut.RenewLockAsync("test-key", "wrong-token", TimeSpan.FromMinutes(10));

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task RenewLockAsync_Fails_WhenLockDoesNotExist()
    {
        var result = await _sut.RenewLockAsync("nonexistent-key", "any-token", TimeSpan.FromMinutes(10));

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task IsLockHeldAsync_ReturnsTrue_WhenLockExists()
    {
        await _sut.AcquireLockAsync("test-key", TimeSpan.FromMinutes(5));

        var held = await _sut.IsLockHeldAsync("test-key");

        held.Should().BeTrue();
    }

    [Fact]
    public async Task IsLockHeldAsync_ReturnsFalse_WhenNoLockExists()
    {
        var held = await _sut.IsLockHeldAsync("nonexistent-key");

        held.Should().BeFalse();
    }

    [Fact]
    public async Task IsLockHeldAsync_ReturnsFalse_AfterRelease()
    {
        var acquired = await _sut.AcquireLockAsync("test-key", TimeSpan.FromMinutes(1));
        await _sut.ReleaseLockAsync("test-key", acquired.Value);

        var held = await _sut.IsLockHeldAsync("test-key");

        held.Should().BeFalse();
    }

    [Fact]
    public async Task FullLifecycle_AcquireRenewRelease()
    {
        // Acquire
        var acquired = await _sut.AcquireLockAsync("lifecycle-key", TimeSpan.FromMinutes(1));
        acquired.IsSuccess.Should().BeTrue();

        // Verify held
        (await _sut.IsLockHeldAsync("lifecycle-key")).Should().BeTrue();

        // Renew
        var renewed = await _sut.RenewLockAsync("lifecycle-key", acquired.Value, TimeSpan.FromMinutes(5));
        renewed.IsSuccess.Should().BeTrue();

        // Still held
        (await _sut.IsLockHeldAsync("lifecycle-key")).Should().BeTrue();

        // Release
        var released = await _sut.ReleaseLockAsync("lifecycle-key", acquired.Value);
        released.IsSuccess.Should().BeTrue();

        // No longer held
        (await _sut.IsLockHeldAsync("lifecycle-key")).Should().BeFalse();

        // Can re-acquire
        var reacquired = await _sut.AcquireLockAsync("lifecycle-key", TimeSpan.FromMinutes(1));
        reacquired.IsSuccess.Should().BeTrue();
    }
}
