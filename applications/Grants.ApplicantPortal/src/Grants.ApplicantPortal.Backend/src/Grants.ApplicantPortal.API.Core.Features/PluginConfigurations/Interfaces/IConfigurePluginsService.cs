namespace Grants.ApplicantPortal.API.Core.Features.PluginConfigurations.Interfaces;

/// <summary>
/// Service for configuring plugins for a contributor
/// </summary>
public interface IConfigurePluginsService
{
  public Task<Result> ResetPluginConfiguration(int pluginId);
}
