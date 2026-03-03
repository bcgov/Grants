using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.BackgroundJobs;

/// <summary>
/// In-memory distributed lock implementation using IDistributedMemoryCache
/// Suitable for single-pod deployments or development scenarios
/// </summary>
public class InMemoryDistributedLock : IDistributedLock
{
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<InMemoryDistributedLock> _logger;
    private static readonly string _instanceId = Environment.MachineName + "_" + Environment.ProcessId;

    public InMemoryDistributedLock(IDistributedCache distributedCache, ILogger<InMemoryDistributedLock> logger)
    {
        _distributedCache = distributedCache;
        _logger = logger;
    }

    public async Task<Result<string>> AcquireLockAsync(
        string key,
        TimeSpan expiry,
        TimeSpan? waitTimeout = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var lockKey = GetLockKey(key);
            var lockToken = GenerateLockToken();
            var deadline = DateTime.UtcNow.Add(waitTimeout ?? TimeSpan.Zero);

            do
            {
                // Try to acquire the lock by setting a value only if it doesn't exist
                var existingLock = await _distributedCache.GetStringAsync(lockKey, cancellationToken);
                
                if (existingLock == null)
                {
                    var lockInfo = new LockInfo
                    {
                        Token = lockToken,
                        AcquiredAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.Add(expiry),
                        InstanceId = _instanceId
                    };

                    var lockInfoJson = JsonSerializer.Serialize(lockInfo);
                    var cacheOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = expiry
                    };

                    await _distributedCache.SetStringAsync(lockKey, lockInfoJson, cacheOptions, cancellationToken);
                    
                    // Verify we actually got the lock (race condition protection)
                    var verifyLock = await _distributedCache.GetStringAsync(lockKey, cancellationToken);
                    if (verifyLock != null)
                    {
                        var verifyLockInfo = JsonSerializer.Deserialize<LockInfo>(verifyLock);
                        if (verifyLockInfo?.Token == lockToken)
                        {
                            _logger.LogDebug("Acquired in-memory distributed lock {Key} with token {Token}", key, lockToken);
                            return Result<string>.Success(lockToken);
                        }
                    }
                }

                if (waitTimeout.HasValue && DateTime.UtcNow < deadline)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
                }
                else
                {
                    break;
                }
            }
            while (!cancellationToken.IsCancellationRequested);

            _logger.LogDebug("Failed to acquire in-memory distributed lock {Key}", key);
            return Result<string>.Error($"Could not acquire lock '{key}' within the specified timeout");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring in-memory distributed lock {Key}", key);
            return Result<string>.Error($"Error acquiring lock: {ex.Message}");
        }
    }

    public async Task<Result> RenewLockAsync(
        string key,
        string token,
        TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var lockKey = GetLockKey(key);
            var existingLockJson = await _distributedCache.GetStringAsync(lockKey, cancellationToken);

            if (existingLockJson == null)
            {
                _logger.LogDebug("Failed to renew in-memory distributed lock {Key} - lock doesn't exist", key);
                return Result.Error($"Could not renew lock '{key}' - lock doesn't exist");
            }

            var existingLock = JsonSerializer.Deserialize<LockInfo>(existingLockJson);
            if (existingLock?.Token != token)
            {
                _logger.LogDebug("Failed to renew in-memory distributed lock {Key} - token mismatch", key);
                return Result.Error($"Could not renew lock '{key}' - token mismatch");
            }

            // Update the lock with new expiry
            var renewedLockInfo = existingLock with { ExpiresAt = DateTime.UtcNow.Add(expiry) };
            var renewedLockJson = JsonSerializer.Serialize(renewedLockInfo);
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry
            };

            await _distributedCache.SetStringAsync(lockKey, renewedLockJson, cacheOptions, cancellationToken);

            _logger.LogDebug("Renewed in-memory distributed lock {Key} with token {Token}", key, token);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renewing in-memory distributed lock {Key}", key);
            return Result.Error($"Error renewing lock: {ex.Message}");
        }
    }

    public async Task<Result> ReleaseLockAsync(
        string key,
        string token,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var lockKey = GetLockKey(key);
            var existingLockJson = await _distributedCache.GetStringAsync(lockKey, cancellationToken);

            if (existingLockJson == null)
            {
                _logger.LogDebug("Failed to release in-memory distributed lock {Key} - lock doesn't exist", key);
                return Result.Error($"Could not release lock '{key}' - lock doesn't exist");
            }

            var existingLock = JsonSerializer.Deserialize<LockInfo>(existingLockJson);
            if (existingLock?.Token != token)
            {
                _logger.LogDebug("Failed to release in-memory distributed lock {Key} - token mismatch", key);
                return Result.Error($"Could not release lock '{key}' - token mismatch");
            }

            await _distributedCache.RemoveAsync(lockKey, cancellationToken);

            _logger.LogDebug("Released in-memory distributed lock {Key} with token {Token}", key, token);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing in-memory distributed lock {Key}", key);
            return Result.Error($"Error releasing lock: {ex.Message}");
        }
    }

    public async Task<bool> IsLockHeldAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var lockKey = GetLockKey(key);
            var existingLockJson = await _distributedCache.GetStringAsync(lockKey, cancellationToken);
            
            if (existingLockJson == null)
                return false;

            var existingLock = JsonSerializer.Deserialize<LockInfo>(existingLockJson);
            return existingLock != null && existingLock.ExpiresAt > DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if in-memory lock is held {Key}", key);
            return false;
        }
    }

    private static string GetLockKey(string key) => $"locks:messaging:{key}";

    private static string GenerateLockToken()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var random = Guid.NewGuid().ToString("N")[..8];
        return $"{_instanceId}:{timestamp}:{random}";
    }

    /// <summary>
    /// Information stored for each distributed lock
    /// </summary>
    private record LockInfo
    {
        public required string Token { get; init; }
        public DateTime AcquiredAt { get; init; }
        public DateTime ExpiresAt { get; init; }
        public required string InstanceId { get; init; }
    }
}
