using Grants.ApplicantPortal.API.Core.Plugins.PluginConfigurations.PluginConfigurationAggregate;

namespace Grants.ApplicantPortal.API.Core.Plugins.PluginConfigurations.Interfaces;

/// <summary>
/// Service interface for managing plugin configurations
/// </summary>
public interface IPluginConfigurationService
{
  Task<PluginConfiguration?> GetConfigurationAsync(string pluginId, CancellationToken cancellationToken = default);
  Task<PluginConfiguration> CreateOrUpdateConfigurationAsync(string pluginId, string configurationJson, string? updatedBy = null, CancellationToken cancellationToken = default);
  Task<bool> DeleteConfigurationAsync(string pluginId, CancellationToken cancellationToken = default);
  Task<IList<PluginConfiguration>> GetAllConfigurationsAsync(CancellationToken cancellationToken = default);
}
