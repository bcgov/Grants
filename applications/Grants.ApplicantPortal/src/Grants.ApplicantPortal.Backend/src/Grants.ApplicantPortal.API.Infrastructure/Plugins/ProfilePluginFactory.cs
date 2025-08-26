using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.Infrastructure.Plugins;

/// <summary>
/// Factory for resolving profile plugins by plugin identifier
/// </summary>
public class ProfilePluginFactory(IServiceProvider serviceProvider, ILogger<ProfilePluginFactory> logger) : IProfilePluginFactory
{
  private readonly Dictionary<string, Type> _pluginTypes = new(StringComparer.OrdinalIgnoreCase);
  private readonly object _lock = new();

  public IProfilePlugin? GetPlugin(string pluginId)
  {
    logger.LogDebug("Resolving plugin for plugin ID: {PluginId}", pluginId);

    if (string.IsNullOrWhiteSpace(pluginId))
    {
      logger.LogWarning("Attempted to resolve plugin with empty plugin ID");
      return null;
    }

    // Try to get from cache first
    if (_pluginTypes.TryGetValue(pluginId, out var cachedType))
    {
      return (IProfilePlugin?)serviceProvider.GetService(cachedType);
    }

    // If not in cache, scan available plugins
    lock (_lock)
    {
      // Double-check pattern
      if (_pluginTypes.TryGetValue(pluginId, out cachedType))
      {
        return (IProfilePlugin?)serviceProvider.GetService(cachedType);
      }

      // Get all registered plugins
      var plugins = serviceProvider.GetServices<IProfilePlugin>();

      foreach (var plugin in plugins)
      {
        var pluginType = plugin.GetType();
        _pluginTypes[plugin.PluginId] = pluginType;

        if (plugin.PluginId.Equals(pluginId, StringComparison.OrdinalIgnoreCase))
        {
          logger.LogDebug("Found plugin {PluginType} for plugin ID: {PluginId}",
              pluginType.Name, pluginId);
          return plugin;
        }
      }
    }

    logger.LogWarning("No plugin found for plugin ID: {PluginId}", pluginId);
    return null;
  }

  public IEnumerable<IProfilePlugin> GetAllPlugins()
  {
    logger.LogDebug("Retrieving all available plugins");
    return serviceProvider.GetServices<IProfilePlugin>();
  }
}
