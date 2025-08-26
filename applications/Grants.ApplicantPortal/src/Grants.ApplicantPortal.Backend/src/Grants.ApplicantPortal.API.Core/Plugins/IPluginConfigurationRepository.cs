using Grants.ApplicantPortal.API.Core.Plugins.PluginConfigurations.PluginConfigurationAggregate;

namespace Grants.ApplicantPortal.API.Core.Plugins;

/// <summary>
/// Repository interface for plugin configurations
/// </summary>
public interface IPluginConfigurationRepository
{
  Task<PluginConfiguration?> GetByPluginIdAsync(string pluginId, CancellationToken cancellationToken = default);
  Task<PluginConfiguration> AddAsync(PluginConfiguration entity, CancellationToken cancellationToken = default);
  Task UpdateAsync(PluginConfiguration entity, CancellationToken cancellationToken = default);
  Task DeleteAsync(PluginConfiguration entity, CancellationToken cancellationToken = default);
  Task<PluginConfiguration?> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken = default) where TId : notnull;
  Task<List<PluginConfiguration>> ListAsync(CancellationToken cancellationToken = default);
}
