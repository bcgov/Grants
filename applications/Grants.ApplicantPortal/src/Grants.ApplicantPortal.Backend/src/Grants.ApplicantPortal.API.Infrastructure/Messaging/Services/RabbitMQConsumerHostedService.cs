using Grants.ApplicantPortal.API.Infrastructure.Messaging.RabbitMQ;
using Microsoft.Extensions.Hosting;

namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.Services;

/// <summary>
/// Hosted service that manages the RabbitMQ consumer lifecycle.
/// Uses a background retry loop with exponential backoff and jitter so the
/// application can start and run in read-only mode while RabbitMQ is unavailable.
/// </summary>
public class RabbitMQConsumerHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMQConsumerHostedService> _logger;
    private RabbitMQConsumer? _consumer;
    private IServiceScope? _serviceScope;

    private static readonly TimeSpan InitialRetryDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(60);
    private const double JitterFactor = 0.3; // ±30% randomisation to avoid thundering herd across pods

    public RabbitMQConsumerHostedService(
        IServiceProvider serviceProvider,
        ILogger<RabbitMQConsumerHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var attempt = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                CleanupPreviousAttempt();

                // Create a scope for scoped services
                _serviceScope = _serviceProvider.CreateScope();

                // Try to get the RabbitMQ consumer from the scope (may not be registered if RabbitMQ is not configured)
                _consumer = _serviceScope.ServiceProvider.GetService<RabbitMQConsumer>();

                if (_consumer == null)
                {
                    _logger.LogInformation("RabbitMQ consumer not available - inbox processing will rely on manual message insertion");
                    return; // Not registered at all — nothing to retry
                }

                _consumer.StartConsuming();
                _logger.LogInformation("RabbitMQ consumer started successfully");

                // Connected — reset attempt counter and stay alive until cancellation
                attempt = 0;
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown — exit the loop
                break;
            }
            catch (Exception ex)
            {
                attempt++;
                var delay = CalculateRetryDelay(attempt);

                _logger.LogWarning(ex,
                    "RabbitMQ consumer connection attempt {Attempt} failed. " +
                    "App continues in read-only mode. Retrying in {Delay:F1}s",
                    attempt, delay.TotalSeconds);

                CleanupPreviousAttempt();

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        _logger.LogInformation("RabbitMQ consumer hosted service stopping");
        CleanupPreviousAttempt();
    }

    /// <summary>
    /// Calculates exponential backoff delay with random jitter to stagger reconnection across pods.
    /// </summary>
    private static TimeSpan CalculateRetryDelay(int attempt)
    {
        // Exponential backoff: 5s, 10s, 20s, 40s, capped at MaxRetryDelay
        var exponentialSeconds = InitialRetryDelay.TotalSeconds * Math.Pow(2, attempt - 1);
        var cappedSeconds = Math.Min(exponentialSeconds, MaxRetryDelay.TotalSeconds);

        // Add jitter: ±JitterFactor (e.g. ±30%)
        var jitter = cappedSeconds * JitterFactor * (2 * Random.Shared.NextDouble() - 1);
        var finalSeconds = Math.Max(1, cappedSeconds + jitter);

        return TimeSpan.FromSeconds(finalSeconds);
    }

    private void CleanupPreviousAttempt()
    {
        try
        {
            _consumer?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing RabbitMQ consumer during cleanup");
        }
        finally
        {
            _consumer = null;
        }

        _serviceScope?.Dispose();
        _serviceScope = null;
    }
}
