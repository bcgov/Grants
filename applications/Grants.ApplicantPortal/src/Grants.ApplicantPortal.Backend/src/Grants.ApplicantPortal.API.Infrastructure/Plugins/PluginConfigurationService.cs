using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.Core.Plugins.PluginConfigurations.Interfaces;
using Grants.ApplicantPortal.API.Core.Plugins.PluginConfigurations.PluginConfigurationAggregate;

namespace Grants.ApplicantPortal.API.Infrastructure.Plugins;

/// <summary>
/// Service implementation for managing plugin configurations
/// </summary>
public class PluginConfigurationService : IPluginConfigurationService
{
  private readonly IPluginConfigurationRepository _repository;
  private readonly ILogger<PluginConfigurationService> _logger;

  public PluginConfigurationService(
      IPluginConfigurationRepository repository,
      ILogger<PluginConfigurationService> logger)
  {
    _repository = repository;
    _logger = logger;
  }

  public async Task<PluginConfiguration?> GetConfigurationAsync(string pluginId, CancellationToken cancellationToken = default)
  {
    _logger.LogDebug("Getting configuration for plugin {PluginId}", pluginId);

    var configuration = await _repository.GetByPluginIdAsync(pluginId, cancellationToken);

    if (configuration == null)
    {
      _logger.LogWarning("Configuration not found for plugin {PluginId}", pluginId);
    }

    return configuration;
  }

  public async Task<PluginConfiguration> CreateOrUpdateConfigurationAsync(
      string pluginId,
      string configurationJson,
      string? updatedBy = null,
      CancellationToken cancellationToken = default)
  {
    _logger.LogInformation("Creating or updating configuration for plugin {PluginId}", pluginId);

    var existingConfiguration = await _repository.GetByPluginIdAsync(pluginId, cancellationToken);

    if (existingConfiguration != null)
    {
      // Update existing configuration
      existingConfiguration.ConfigurationJson = configurationJson;
      existingConfiguration.UpdatedAt = DateTime.UtcNow;
      existingConfiguration.UpdatedBy = updatedBy;

      await _repository.UpdateAsync(existingConfiguration, cancellationToken);

      _logger.LogInformation("Updated configuration for plugin {PluginId}", pluginId);
      return existingConfiguration;
    }
    else
    {
      // Create new configuration
      var newConfiguration = new PluginConfiguration
      {
        Id = Guid.NewGuid(),
        PluginId = pluginId,
        ConfigurationJson = configurationJson,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        CreatedBy = updatedBy,
        UpdatedBy = updatedBy,
        IsActive = true
      };

      await _repository.AddAsync(newConfiguration, cancellationToken);

      _logger.LogInformation("Created new configuration for plugin {PluginId}", pluginId);
      return newConfiguration;
    }
  }

  public async Task<bool> DeleteConfigurationAsync(string pluginId, CancellationToken cancellationToken = default)
  {
    _logger.LogInformation("Deleting configuration for plugin {PluginId}", pluginId);

    var configuration = await _repository.GetByPluginIdAsync(pluginId, cancellationToken);

    if (configuration == null)
    {
      _logger.LogWarning("Configuration not found for plugin {PluginId}, cannot delete", pluginId);
      return false;
    }

    // Soft delete by marking as inactive
    configuration.IsActive = false;
    configuration.UpdatedAt = DateTime.UtcNow;

    await _repository.UpdateAsync(configuration, cancellationToken);

    _logger.LogInformation("Soft deleted configuration for plugin {PluginId}", pluginId);
    return true;
  }

  public async Task<IList<PluginConfiguration>> GetAllConfigurationsAsync(CancellationToken cancellationToken = default)
  {
    _logger.LogDebug("Getting all plugin configurations");

    var configurations = await _repository.ListAsync(cancellationToken);
    return configurations.Where(c => c.IsActive).ToList();
  }
}
