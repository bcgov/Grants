using Grants.ApplicantPortal.API.Core.Features.PluginConfigurations.Interfaces;
using Grants.ApplicantPortal.API.Core.Features.PluginConfigurations.PluginConfigurationAggregate;
using Grants.ApplicantPortal.API.Core.Features.PluginConfigurations.PluginConfigurationAggregate.Events;

namespace Grants.ApplicantPortal.API.Core.Features.PluginConfigurations.Services;

/// <summary>
/// Service for configuring plugins
/// </summary>
public class PluginConfigurationService(IRepository<PluginConfiguration> _repository,
  IMediator _mediator,
  ILogger<PluginConfigurationService> _logger) : IConfigurePluginsService
{
  public async Task<Result> ResetPluginConfiguration(int pluginId)
  {
    _logger.LogInformation("Resetting Plugin {pluginId}", pluginId);
    var plugin = await _repository.GetByIdAsync(pluginId);
    if (plugin == null) return Result.NotFound();

    plugin.ConfigurationJson = "{}";
    await _repository.UpdateAsync(plugin);

    var domainEvent = new PluginConfigurationResetEvent(pluginId);
    await _mediator.Publish(domainEvent);

    return Result.Success();
  }
}
