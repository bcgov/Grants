using System.Collections.Concurrent;
using Grants.ApplicantPortal.API.Infrastructure.Messaging.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.BackgroundJobs;

/// <summary>
/// Tracks consecutive failures per background job and provides exponential backoff
/// with log suppression to prevent log flooding when infrastructure is unavailable.
/// Registered as a singleton so state persists across Quartz job executions.
/// </summary>
public sealed class JobCircuitBreaker
{
    private readonly ConcurrentDictionary<string, JobState> _states = new();
    private readonly BackgroundJobOptions _options;
    private readonly ILogger<JobCircuitBreaker> _logger;

    public JobCircuitBreaker(IOptions<MessagingOptions> options, ILogger<JobCircuitBreaker> logger)
    {
        _options = options.Value.BackgroundJobs;
        _logger = logger;
    }

    /// <summary>
    /// Returns true if the job should execute on this tick.
    /// Returns false if the backoff period has not elapsed yet (job should skip).
    /// </summary>
    public bool ShouldExecute(string jobKey)
    {
        var state = _states.GetOrAdd(jobKey, _ => new JobState());

        if (state.ConsecutiveFailures == 0)
        {
            return true;
        }

        if (DateTime.UtcNow >= state.NextAllowedAttemptUtc)
        {
            return true;
        }

        _logger.LogDebug(
            "{JobKey} circuit open — skipping execution ({Failures} consecutive failures, next attempt at {NextAttempt:HH:mm:ss})",
            jobKey, state.ConsecutiveFailures, state.NextAllowedAttemptUtc);

        return false;
    }

    /// <summary>
    /// Records a successful execution — resets the breaker to closed state.
    /// </summary>
    public void RecordSuccess(string jobKey)
    {
        var state = _states.GetOrAdd(jobKey, _ => new JobState());
        var previousFailures = state.ConsecutiveFailures;

        if (previousFailures > 0)
        {
            _logger.LogInformation(
                "{JobKey} circuit recovered after {Failures} consecutive failures",
                jobKey, previousFailures);
        }

        state.Reset();
    }

    /// <summary>
    /// Records a failed execution — increases backoff and conditionally logs the error.
    /// First failure and every Nth failure log at Error/Warning; others are suppressed to Debug.
    /// </summary>
    public void RecordFailure(string jobKey, Exception exception)
    {
        var state = _states.GetOrAdd(jobKey, _ => new JobState());
        state.ConsecutiveFailures++;

        var baseIntervalSeconds = _options.BaseBackoffSeconds;
        var multiplier = _options.BackoffMultiplier;
        var maxBackoff = _options.MaxBackoffSeconds;
        var logEveryNth = _options.LogEveryNthFailure;

        // Exponential backoff: base * multiplier^(failures-1), capped at max
        var backoffSeconds = Math.Min(
            baseIntervalSeconds * Math.Pow(multiplier, state.ConsecutiveFailures - 1),
            maxBackoff);

        state.NextAllowedAttemptUtc = DateTime.UtcNow.AddSeconds(backoffSeconds);

        // Log strategy: infrastructure failures (Redis/connectivity) at Warning; code failures at Error.
        // First failure always logged; every Nth failure logged during sustained outage; others suppressed.
        var isInfrastructureFailure = exception is DistributedLockException;

        if (state.ConsecutiveFailures == 1)
        {
            if (isInfrastructureFailure)
            {
                _logger.LogWarning(exception,
                    "{JobKey} infrastructure unavailable — circuit opened. Next attempt in {Backoff}s",
                    jobKey, (int)backoffSeconds);
            }
            else
            {
                _logger.LogError(exception,
                    "{JobKey} failed — circuit opened. Next attempt in {Backoff}s",
                    jobKey, (int)backoffSeconds);
            }
        }
        else if (state.ConsecutiveFailures % logEveryNth == 0)
        {
            _logger.LogWarning(
                "{JobKey} still failing — {Failures} consecutive failures. Next attempt in {Backoff}s. Error: {Error}",
                jobKey, state.ConsecutiveFailures, (int)backoffSeconds, exception.Message);
        }
        else
        {
            _logger.LogDebug(
                "{JobKey} failed ({Failures} consecutive). Next attempt in {Backoff}s",
                jobKey, state.ConsecutiveFailures, (int)backoffSeconds);
        }
    }

    private sealed class JobState
    {
        public int ConsecutiveFailures;
        public DateTime NextAllowedAttemptUtc = DateTime.MinValue;

        public void Reset()
        {
            ConsecutiveFailures = 0;
            NextAllowedAttemptUtc = DateTime.MinValue;
        }
    }
}
