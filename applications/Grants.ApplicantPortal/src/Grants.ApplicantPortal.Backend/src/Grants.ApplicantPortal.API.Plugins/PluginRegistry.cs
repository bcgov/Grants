using System.Collections.Concurrent;
using Grants.ApplicantPortal.API.Core.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Grants.ApplicantPortal.API.Plugins;

/// <summary>
/// Static registry for caching plugin information at application startup
/// Provides fast plugin validation without dependency injection overhead
/// </summary>
public static class PluginRegistry
{
  private static readonly ConcurrentDictionary<string, PluginInfo> _plugins = new(StringComparer.OrdinalIgnoreCase);
  private static bool _isInitialized = false;
  private static readonly object _initLock = new();
  private static PluginConfiguration? _configuration;

  /// <summary>
  /// Information about a registered plugin with its configuration
  /// </summary>
  public record PluginInfo(
      string PluginId,
      Type PluginType,
      string Description,
      IReadOnlyList<string> SupportedFeatures,
      PluginOptions? Configuration = null);

  /// <summary>
  /// Initialize the plugin registry with available plugins and configuration
  /// Should be called during application startup
  /// </summary>
  public static void Initialize(IServiceProvider serviceProvider, PluginConfiguration? configuration = null)
  {
    if (_isInitialized) return;

    lock (_initLock)
    {
      if (_isInitialized) return;

      _configuration = configuration;
      var plugins = serviceProvider.GetServices<IProfilePlugin>();

      foreach (var plugin in plugins)
      {
        var pluginType = plugin.GetType();
        var description = pluginType.Name.Replace("Plugin", "").Replace("Profile", "");
        
        // Get configuration for this plugin if available
        PluginOptions? pluginConfig = null;
        _configuration?.TryGetValue(plugin.PluginId, out pluginConfig);

        _plugins[plugin.PluginId] = new PluginInfo(
            plugin.PluginId,
            pluginType,
            description,
            plugin.GetSupportedFeatures(),
            pluginConfig);
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
  /// Get all configured plugins (only those with configuration)
  /// </summary>
  /// <param name="enabledOnly">If true, only return enabled plugins</param>
  public static IEnumerable<PluginInfo> GetConfiguredPlugins(bool enabledOnly = true)
  {
    return _plugins.Values
      .Where(p => p.Configuration != null)
      .Where(p => !enabledOnly || p.Configuration!.Enabled)
      .OrderBy(p => p.PluginId);
  }

  /// <summary>
  /// Get configured plugin by ID
  /// </summary>
  /// <param name="pluginId">Plugin ID</param>
  /// <param name="enabledOnly">If true, only return if enabled</param>
  public static PluginInfo? GetConfiguredPlugin(string pluginId, bool enabledOnly = true)
  {
    if (!_plugins.TryGetValue(pluginId, out var plugin))
      return null;
      
    if (plugin.Configuration == null)
      return null;
      
    if (enabledOnly && !plugin.Configuration.Enabled)
      return null;
      
    return plugin;
  }

  /// <summary>
  /// Check if the registry has been initialized
  /// </summary>
  public static bool IsInitialized => _isInitialized;
}
