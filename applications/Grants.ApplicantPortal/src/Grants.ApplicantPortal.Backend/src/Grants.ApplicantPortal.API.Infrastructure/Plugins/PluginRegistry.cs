using System.Collections.Concurrent;
using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.Infrastructure.Plugins;

/// <summary>
/// Static registry for caching plugin information at application startup
/// Provides fast plugin validation without dependency injection overhead
/// </summary>
public static class PluginRegistry
{
    private static readonly ConcurrentDictionary<string, PluginInfo> _plugins = new(StringComparer.OrdinalIgnoreCase);
    private static bool _isInitialized = false;
    private static readonly object _initLock = new();

    /// <summary>
    /// Information about a registered plugin
    /// </summary>
    public record PluginInfo(string PluginId, Type PluginType, string Description);

    /// <summary>
    /// Initialize the plugin registry with available plugins
    /// Should be called during application startup
    /// </summary>
    public static void Initialize(IServiceProvider serviceProvider)
    {
        if (_isInitialized) return;

        lock (_initLock)
        {
            if (_isInitialized) return;

            var plugins = serviceProvider.GetServices<IProfilePlugin>();
            
            foreach (var plugin in plugins)
            {
                var pluginType = plugin.GetType();
                var description = pluginType.Name.Replace("Plugin", "").Replace("Profile", "");
                
                _plugins[plugin.PluginId] = new PluginInfo(
                    plugin.PluginId, 
                    pluginType, 
                    description);
            }

            _isInitialized = true;
        }
    }

    /// <summary>
    /// Check if a plugin ID is valid (registered)
    /// </summary>
    public static bool IsValidPluginId(string? pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId)) return false;
        return _plugins.ContainsKey(pluginId);
    }

    /// <summary>
    /// Get all registered plugin IDs
    /// </summary>
    public static IEnumerable<string> GetAllPluginIds()
    {
        return _plugins.Keys;
    }

    /// <summary>
    /// Get plugin information by plugin ID
    /// </summary>
    public static PluginInfo? GetPluginInfo(string pluginId)
    {
        _plugins.TryGetValue(pluginId, out var info);
        return info;
    }

    /// <summary>
    /// Get all registered plugins information
    /// </summary>
    public static IEnumerable<PluginInfo> GetAllPlugins()
    {
        return _plugins.Values;
    }

    /// <summary>
    /// Check if the registry has been initialized
    /// </summary>
    public static bool IsInitialized => _isInitialized;
}
