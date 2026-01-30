namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.BackgroundJobs;

/// <summary>
/// Interface for distributed locking to prevent multiple pods from processing the same work
/// </summary>
public interface IDistributedLock
{
    /// <summary>
    /// Attempts to acquire a lock with the specified key
    /// </summary>
    /// <param name="key">Unique key for the lock</param>
    /// <param name="expiry">How long the lock should be held before automatic expiry</param>
    /// <param name="waitTimeout">How long to wait for the lock if it's not immediately available</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Lock token if successful, or error if lock could not be acquired</returns>
    Task<Result<string>> AcquireLockAsync(
        string key, 
        TimeSpan expiry, 
        TimeSpan? waitTimeout = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renews an existing lock
    /// </summary>
    /// <param name="key">Lock key</param>
    /// <param name="token">Lock token</param>
    /// <param name="expiry">New expiry time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> RenewLockAsync(
        string key, 
        string token, 
        TimeSpan expiry, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a lock
    /// </summary>
    /// <param name="key">Lock key</param>
    /// <param name="token">Lock token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> ReleaseLockAsync(
        string key, 
        string token, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a lock exists and is still valid
    /// </summary>
    /// <param name="key">Lock key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if lock exists and is valid</returns>
    Task<bool> IsLockHeldAsync(string key, CancellationToken cancellationToken = default);
}
