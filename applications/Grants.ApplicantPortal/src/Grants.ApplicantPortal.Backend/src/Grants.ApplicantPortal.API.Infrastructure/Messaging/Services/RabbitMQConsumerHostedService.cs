using Grants.ApplicantPortal.API.Infrastructure.Messaging.RabbitMQ;
using Microsoft.Extensions.Hosting;

namespace Grants.ApplicantPortal.API.Infrastructure.Messaging.Services;

/// <summary>
/// Hosted service that manages the RabbitMQ consumer lifecycle
/// </summary>
public class RabbitMQConsumerHostedService : IHostedService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RabbitMQConsumerHostedService> _logger;
    private RabbitMQConsumer? _consumer;
    private IServiceScope? _serviceScope;
    private bool _disposed = false;

    public RabbitMQConsumerHostedService(
        IServiceProvider serviceProvider,
        ILogger<RabbitMQConsumerHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Create a scope for scoped services
            _serviceScope = _serviceProvider.CreateScope();
            
            // Try to get the RabbitMQ consumer from the scope (may not be registered if RabbitMQ is not configured)
            _consumer = _serviceScope.ServiceProvider.GetService<RabbitMQConsumer>();
            
            if (_consumer != null)
            {
                _consumer.StartConsuming();
                _logger.LogInformation("RabbitMQ consumer started successfully");
            }
            else
            {
                _logger.LogInformation("RabbitMQ consumer not available - inbox processing will rely on manual message insertion");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start RabbitMQ consumer");
            
            // Clean up the scope if startup failed
            _serviceScope?.Dispose();
            _serviceScope = null;
            
            throw;
        }

        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_consumer != null)
        {
            try
            {
                _consumer.StopConsuming();
                _logger.LogInformation("RabbitMQ consumer stopped successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error stopping RabbitMQ consumer");
            }
        }
        else
        {
            _logger.LogInformation("RabbitMQ consumer service stopped (no consumer configured)");
        }

        await Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            _consumer?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing RabbitMQ consumer");
        }

        _serviceScope?.Dispose();
        _disposed = true;
        
        _logger.LogDebug("RabbitMQ consumer hosted service disposed");
    }
}
