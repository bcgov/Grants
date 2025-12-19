using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.Plugins.Services;

/// <summary>
/// Service that seeds all plugin data on application startup
/// </summary>
public class PluginDataSeedingService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PluginDataSeedingService> _logger;

    public PluginDataSeedingService(
        IServiceProvider serviceProvider,
        ILogger<PluginDataSeedingService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting plugin data seeding");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var pluginFactory = scope.ServiceProvider.GetRequiredService<IProfilePluginFactory>();
            var allPlugins = pluginFactory.GetAllPlugins().ToList();

            _logger.LogInformation("Found {PluginCount} plugins to seed", allPlugins.Count);

            foreach (var plugin in allPlugins)
            {
                try
                {
                    _logger.LogInformation("Seeding data for plugin: {PluginId}", plugin.PluginId);
                    
                    var startTime = DateTimeOffset.UtcNow;
                    await plugin.SeedDataAsync(cancellationToken);
                    var duration = DateTimeOffset.UtcNow - startTime;
                    
                    _logger.LogInformation("Plugin {PluginId} seeding completed in {Duration}ms", 
                        plugin.PluginId, Math.Round(duration.TotalMilliseconds, 1));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to seed data for plugin: {PluginId}", plugin.PluginId);
                    // Continue with other plugins - don't fail startup for seeding issues
                }
            }

            _logger.LogInformation("Plugin data seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error during plugin data seeding");
            // Don't throw - seeding failure shouldn't prevent startup
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
