using System.Collections.Concurrent;
using Grants.ApplicantPortal.API.Core.Plugins;
using Microsoft.Extensions.DependencyInjection;

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

  /// <summary>
  /// Information about a registered plugin
  /// </summary>
  public record PluginInfo(
      string PluginId,
      Type PluginType,
      string Description,
      IReadOnlyList<PluginSupportedFeature> SupportedFeatures);

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
            description,
            plugin.GetSupportedFeatures());
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
  /// Check if a plugin supports a specific provider/key combination
  /// </summary>
  public static bool IsValidProviderKey(string? pluginId, string? provider, string? key)
  {
    if (string.IsNullOrWhiteSpace(pluginId) || string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(key))
      return false;

    if (!_plugins.TryGetValue(pluginId, out var pluginInfo))
      return false;

    return pluginInfo.SupportedFeatures.Any(f =>
        f.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase) &&
        f.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
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
  /// Get supported features for a specific plugin
  /// </summary>
  public static IReadOnlyList<PluginSupportedFeature> GetSupportedFeatures(string pluginId)
  {
    if (_plugins.TryGetValue(pluginId, out var pluginInfo))
      return pluginInfo.SupportedFeatures;

    return new List<PluginSupportedFeature>();
  }

  /// <summary>
  /// Get all supported providers across all plugins
  /// </summary>
  public static IReadOnlyList<string> GetAllSupportedProviders()
  {
    return _plugins.Values
        .SelectMany(p => p.SupportedFeatures)
        .Select(f => f.Provider)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(p => p)
        .ToList();
  }

  /// <summary>
  /// Check if the registry has been initialized
  /// </summary>
  public static bool IsInitialized => _isInitialized;
}
