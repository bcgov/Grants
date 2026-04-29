namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.BackgroundJobs;

/// <summary>
/// Thrown by background jobs when a distributed lock operation fails due to an
/// infrastructure problem (e.g. Redis unavailable). Carries the original infrastructure
/// exception as <see cref="Exception.InnerException"/> so the <see cref="JobCircuitBreaker"/>
/// can classify it as a transient infrastructure failure rather than a code bug.
/// </summary>
public sealed class DistributedLockException : Exception
{
    public DistributedLockException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}
