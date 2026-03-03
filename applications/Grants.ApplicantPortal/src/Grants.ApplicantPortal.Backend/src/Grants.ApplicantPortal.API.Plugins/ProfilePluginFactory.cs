using Grants.ApplicantPortal.API.Core.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Grants.ApplicantPortal.API.Plugins;

/// <summary>
/// Factory for resolving profile plugins by plugin identifier
/// </summary>
public class ProfilePluginFactory(IServiceProvider serviceProvider, ILogger<ProfilePluginFactory> logger) : IProfilePluginFactory
{

  public IProfilePlugin? GetPlugin(string pluginId)
  {
    logger.LogDebug("Resolving plugin for plugin ID: {PluginId}", pluginId);

    if (string.IsNullOrWhiteSpace(pluginId))
    {
      logger.LogWarning("Attempted to resolve plugin with empty plugin ID");
      return null;
    }

    // Get all registered plugins and find the one with matching ID
    var plugins = serviceProvider.GetServices<IProfilePlugin>();
    
    foreach (var plugin in plugins)
    {
      if (plugin.PluginId.Equals(pluginId, StringComparison.OrdinalIgnoreCase))
      {
        logger.LogDebug("Found plugin {PluginType} for plugin ID: {PluginId}",
            plugin.GetType().Name, pluginId);
        return plugin;
      }
    }

    logger.LogWarning("No plugin found for plugin ID: {PluginId}. Available plugins: {AvailablePlugins}", 
        pluginId, string.Join(", ", plugins.Select(p => p.PluginId)));
    return null;
  }

  public IEnumerable<IProfilePlugin> GetAllPlugins()
  {
    logger.LogDebug("Retrieving all available plugins");
    return serviceProvider.GetServices<IProfilePlugin>();
  }
}
