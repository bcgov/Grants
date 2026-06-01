using FluentAssertions;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.BackgroundJobs;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using StackExchange.Redis;

namespace Grants.ApplicantPortal.API.UnitTests.Infrastructure;

public class RedisDistributedLockTests
{
    private readonly IConnectionMultiplexer _mux = Substitute.For<IConnectionMultiplexer>();
    private readonly IDatabase _db = Substitute.For<IDatabase>();
    private readonly RedisDistributedLock _sut;

    public RedisDistributedLockTests()
    {
        _mux.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(_db);
        _sut = new RedisDistributedLock(_mux, NullLogger<RedisDistributedLock>.Instance);
    }

    // ── AcquireLockAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task AcquireLockAsync_ReturnsError_OnRedisConnectionException()
    {
        _mux.GetDatabase(Arg.Any<int>(), Arg.Any<object?>())
            .Throws(new RedisConnectionException(ConnectionFailureType.SocketFailure, "socket closed"));

        var result = await _sut.AcquireLockAsync("key", TimeSpan.FromMinutes(1));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainMatch("*unavailable*");
    }

    [Fact]
    public async Task AcquireLockAsync_ReturnsError_OnRedisTimeoutException()
    {
        _mux.GetDatabase(Arg.Any<int>(), Arg.Any<object?>())
            .Throws(new RedisTimeoutException("timed out", CommandStatus.Unknown));

        var result = await _sut.AcquireLockAsync("key", TimeSpan.FromMinutes(1));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainMatch("*timed out*");
    }

    [Fact]
    public async Task AcquireLockAsync_ReturnsError_OnReadonlyServerException()
    {
        _mux.GetDatabase(Arg.Any<int>(), Arg.Any<object?>())
            .Throws(new RedisServerException("READONLY You can't write against a read only replica"));

        var result = await _sut.AcquireLockAsync("key", TimeSpan.FromMinutes(1));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainMatch("*READONLY*");
    }

    [Fact]
    public async Task AcquireLockAsync_ReturnsError_OnGenericRedisException()
    {
        _mux.GetDatabase(Arg.Any<int>(), Arg.Any<object?>())
            .Throws(new RedisServerException("ERR unknown command"));

        var result = await _sut.AcquireLockAsync("key", TimeSpan.FromMinutes(1));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainMatch("*Redis error*");
    }

    // ── RenewLockAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task RenewLockAsync_ReturnsError_OnRedisConnectionException()
    {
        _mux.GetDatabase(Arg.Any<int>(), Arg.Any<object?>())
            .Throws(new RedisConnectionException(ConnectionFailureType.SocketFailure, "socket closed"));

        var result = await _sut.RenewLockAsync("key", "token", TimeSpan.FromMinutes(1));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainMatch("*unavailable*");
    }

    [Fact]
    public async Task RenewLockAsync_ReturnsError_OnRedisTimeoutException()
    {
        _mux.GetDatabase(Arg.Any<int>(), Arg.Any<object?>())
            .Throws(new RedisTimeoutException("timed out", CommandStatus.Unknown));

        var result = await _sut.RenewLockAsync("key", "token", TimeSpan.FromMinutes(1));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainMatch("*timed out*");
    }

    [Fact]
    public async Task RenewLockAsync_ReturnsError_OnReadonlyServerException()
    {
        _mux.GetDatabase(Arg.Any<int>(), Arg.Any<object?>())
            .Throws(new RedisServerException("READONLY You can't write against a read only replica"));

        var result = await _sut.RenewLockAsync("key", "token", TimeSpan.FromMinutes(1));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainMatch("*READONLY*");
    }

    [Fact]
    public async Task RenewLockAsync_ReturnsError_OnGenericRedisException()
    {
        _mux.GetDatabase(Arg.Any<int>(), Arg.Any<object?>())
            .Throws(new RedisServerException("ERR unknown command"));

        var result = await _sut.RenewLockAsync("key", "token", TimeSpan.FromMinutes(1));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainMatch("*Redis error*");
    }

    // ── ReleaseLockAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task ReleaseLockAsync_ReturnsError_OnRedisConnectionException()
    {
        _mux.GetDatabase(Arg.Any<int>(), Arg.Any<object?>())
            .Throws(new RedisConnectionException(ConnectionFailureType.SocketFailure, "socket closed"));

        var result = await _sut.ReleaseLockAsync("key", "token");

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainMatch("*unavailable*");
    }

    [Fact]
    public async Task ReleaseLockAsync_ReturnsError_OnRedisTimeoutException()
    {
        _mux.GetDatabase(Arg.Any<int>(), Arg.Any<object?>())
            .Throws(new RedisTimeoutException("timed out", CommandStatus.Unknown));

        var result = await _sut.ReleaseLockAsync("key", "token");

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainMatch("*timed out*");
    }

    [Fact]
    public async Task ReleaseLockAsync_ReturnsError_OnReadonlyServerException()
    {
        _mux.GetDatabase(Arg.Any<int>(), Arg.Any<object?>())
            .Throws(new RedisServerException("READONLY You can't write against a read only replica"));

        var result = await _sut.ReleaseLockAsync("key", "token");

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainMatch("*READONLY*");
    }

    [Fact]
    public async Task ReleaseLockAsync_ReturnsError_OnGenericRedisException()
    {
        _mux.GetDatabase(Arg.Any<int>(), Arg.Any<object?>())
            .Throws(new RedisServerException("ERR unknown command"));

        var result = await _sut.ReleaseLockAsync("key", "token");

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainMatch("*Redis error*");
    }

    // ── IsLockHeldAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task IsLockHeldAsync_ReturnsFalse_OnRedisConnectionException()
    {
        _mux.GetDatabase(Arg.Any<int>(), Arg.Any<object?>())
            .Throws(new RedisConnectionException(ConnectionFailureType.SocketFailure, "socket closed"));

        var result = await _sut.IsLockHeldAsync("key");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsLockHeldAsync_ReturnsFalse_OnRedisTimeoutException()
    {
        _mux.GetDatabase(Arg.Any<int>(), Arg.Any<object?>())
            .Throws(new RedisTimeoutException("timed out", CommandStatus.Unknown));

        var result = await _sut.IsLockHeldAsync("key");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsLockHeldAsync_ReturnsFalse_OnReadonlyServerException()
    {
        _mux.GetDatabase(Arg.Any<int>(), Arg.Any<object?>())
            .Throws(new RedisServerException("READONLY You can't write against a read only replica"));

        var result = await _sut.IsLockHeldAsync("key");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsLockHeldAsync_ReturnsFalse_OnGenericRedisException()
    {
        _mux.GetDatabase(Arg.Any<int>(), Arg.Any<object?>())
            .Throws(new RedisServerException("ERR unknown command"));

        var result = await _sut.IsLockHeldAsync("key");

        result.Should().BeFalse();
    }
}
