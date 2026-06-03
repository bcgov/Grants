using System.Security.Cryptography;
using System.Text;
using StackExchange.Redis;

namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.BackgroundJobs;

/// <summary>
/// Redis-based distributed lock implementation
/// </summary>
public class RedisDistributedLock(IConnectionMultiplexer connectionMultiplexer, 
  ILogger<RedisDistributedLock> logger) : IDistributedLock
{
  private static readonly string _instanceId = Environment.MachineName + "_" + Environment.ProcessId;
  private const string ReadonlyError = "READONLY";

  // Lua script for atomic lock release
  private const string ReleaseLockScript = @"
        if redis.call('get', KEYS[1]) == ARGV[1] then
            return redis.call('del', KEYS[1])
        else
            return 0
        end";

  // Lua script for atomic lock renewal
  private const string RenewLockScript = @"
        if redis.call('get', KEYS[1]) == ARGV[1] then
            return redis.call('pexpire', KEYS[1], ARGV[2])
        else
            return 0
        end";

  public async Task<Result<string>> AcquireLockAsync(
      string key,
      TimeSpan expiry,
      TimeSpan? waitTimeout = null,
      CancellationToken cancellationToken = default)
  {
    try
    {
      var database = connectionMultiplexer.GetDatabase();
      var lockKey = GetLockKey(key);
      var lockToken = GenerateLockToken();

      var deadline = DateTime.UtcNow.Add(waitTimeout ?? TimeSpan.Zero);

      do
      {
        var acquired = await database.StringSetAsync(lockKey, lockToken, expiry, When.NotExists);

        if (acquired)
        {
          logger.LogDebug("Acquired distributed lock {Key} with token {Token}", key, lockToken);
          return Result<string>.Success(lockToken);
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

      logger.LogDebug("Failed to acquire distributed lock {Key}", key);
      return Result<string>.Error($"Could not acquire lock '{key}' within the specified timeout");
    }
    catch (RedisConnectionException ex)
    {
      // transient connectivity loss (e.g. Redis pod recycled overnight)
      logger.LogWarning(ex, "Redis unavailable while acquiring distributed lock {Key}", key);
      return Result<string>.Error("Redis unavailable while acquiring lock");
    }
    catch (RedisTimeoutException ex)
    {
      logger.LogWarning(ex, "Redis command timed out while acquiring distributed lock {Key}", key);
      return Result<string>.Error("Redis timed out while acquiring lock");
    }
    catch (RedisServerException ex) when (ex.Message.Contains(ReadonlyError, StringComparison.OrdinalIgnoreCase))
    {
      // Sentinel failover in progress — old master is now a replica
      logger.LogWarning(ex, "Redis is READONLY while acquiring distributed lock {Key} (Sentinel failover in progress)", key);
      return Result<string>.Error("Redis unavailable (READONLY) while acquiring lock");
    }
    catch (RedisException ex)
    {
      logger.LogWarning(ex, "Redis error while acquiring distributed lock {Key}", key);
      return Result<string>.Error("Redis error while acquiring lock");
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error acquiring distributed lock {Key}", key);
      return Result<string>.Error("Error acquiring lock");
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
      var database = connectionMultiplexer.GetDatabase();
      var lockKey = GetLockKey(key);

      var result = await database.ScriptEvaluateAsync(
          RenewLockScript,
          [lockKey],
          [token, (int)expiry.TotalMilliseconds]);

      if ((int)result == 1)
      {
        logger.LogDebug("Renewed distributed lock {Key} with token {Token}", key, token);
        return Result.Success();
      }

      logger.LogDebug("Failed to renew distributed lock {Key} - token mismatch or lock doesn't exist", key);
      return Result.Error($"Could not renew lock '{key}' - token mismatch or lock doesn't exist");
    }
    catch (RedisConnectionException ex)
    {
      logger.LogWarning(ex, "Redis unavailable while renewing distributed lock {Key}", key);
      return Result.Error("Redis unavailable while renewing lock");
    }
    catch (RedisTimeoutException ex)
    {
      logger.LogWarning(ex, "Redis command timed out while renewing distributed lock {Key}", key);
      return Result.Error("Redis timed out while renewing lock");
    }
    catch (RedisServerException ex) when (ex.Message.Contains(ReadonlyError, StringComparison.OrdinalIgnoreCase))
    {
      // Sentinel failover in progress — old master is now a replica
      logger.LogWarning(ex, "Redis is READONLY while renewing distributed lock {Key} (Sentinel failover in progress)", key);
      return Result.Error("Redis unavailable (READONLY) while renewing lock");
    }
    catch (RedisException ex)
    {
      logger.LogWarning(ex, "Redis error while renewing distributed lock {Key}", key);
      return Result.Error("Redis error while renewing lock");
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error renewing distributed lock {Key}", key);
      return Result.Error("Error renewing lock");
    }
  }

  public async Task<Result> ReleaseLockAsync(
      string key,
      string token,
      CancellationToken cancellationToken = default)
  {
    try
    {
      var database = connectionMultiplexer.GetDatabase();
      var lockKey = GetLockKey(key);

      var result = await database.ScriptEvaluateAsync(
          ReleaseLockScript,
          [lockKey],
          [token]);

      if ((int)result == 1)
      {
        logger.LogDebug("Released distributed lock {Key} with token {Token}", key, token);
        return Result.Success();
      }

      logger.LogDebug("Failed to release distributed lock {Key} - token mismatch or lock doesn't exist", key);
      return Result.Error($"Could not release lock '{key}' - token mismatch or lock doesn't exist");
    }
    catch (RedisConnectionException ex)
    {
      logger.LogWarning(ex, "Redis unavailable while releasing distributed lock {Key}", key);
      return Result.Error("Redis unavailable while releasing lock");
    }
    catch (RedisTimeoutException ex)
    {
      logger.LogWarning(ex, "Redis command timed out while releasing distributed lock {Key}", key);
      return Result.Error("Redis timed out while releasing lock");
    }
    catch (RedisServerException ex) when (ex.Message.Contains(ReadonlyError, StringComparison.OrdinalIgnoreCase))
    {
      // Sentinel failover in progress — old master is now a replica
      logger.LogWarning(ex, "Redis is READONLY while releasing distributed lock {Key} (Sentinel failover in progress)", key);
      return Result.Error("Redis unavailable (READONLY) while releasing lock");
    }
    catch (RedisException ex)
    {
      logger.LogWarning(ex, "Redis error while releasing distributed lock {Key}", key);
      return Result.Error("Redis error while releasing lock");
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error releasing distributed lock {Key}", key);
      return Result.Error("Error releasing lock");
    }
  }

  public async Task<bool> IsLockHeldAsync(string key, CancellationToken cancellationToken = default)
  {
    try
    {
      var database = connectionMultiplexer.GetDatabase();
      var lockKey = GetLockKey(key);

      var exists = await database.KeyExistsAsync(lockKey);
      return exists;
    }
    catch (RedisConnectionException ex)
    {
      logger.LogWarning(ex, "Redis unavailable while checking distributed lock {Key}", key);
      return false;
    }
    catch (RedisTimeoutException ex)
    {
      logger.LogWarning(ex, "Redis command timed out while checking distributed lock {Key}", key);
      return false;
    }
    catch (RedisServerException ex) when (ex.Message.Contains(ReadonlyError, StringComparison.OrdinalIgnoreCase))
    {
      logger.LogWarning(ex, "Redis is READONLY while checking distributed lock {Key} (Sentinel failover in progress)", key);
      return false;
    }
    catch (RedisException ex)
    {
      logger.LogWarning(ex, "Redis error while checking distributed lock {Key}", key);
      return false;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error checking if lock is held {Key}", key);
      return false;
    }
  }

  private static string GetLockKey(string key) => $"locks:messaging:{key}";

  private static string GenerateLockToken()
  {
    var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    var random = RandomNumberGenerator.GetBytes(8);
    var randomHex = Convert.ToHexString(random).ToLowerInvariant();

    return $"{_instanceId}:{timestamp}:{randomHex}";
  }
}
