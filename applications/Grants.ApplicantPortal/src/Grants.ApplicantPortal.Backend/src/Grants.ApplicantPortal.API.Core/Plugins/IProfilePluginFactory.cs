namespace Grants.ApplicantPortal.API.Core.Plugins;

/// <summary>
/// Plugin factory for resolving plugins by plugin identifier
/// </summary>
public interface IProfilePluginFactory
{
  /// <summary>
  /// Gets a plugin by plugin identifier
  /// </summary>
  IProfilePlugin? GetPlugin(string pluginId);

  /// <summary>
  /// Gets all available plugins
  /// </summary>
  IEnumerable<IProfilePlugin> GetAllPlugins();
}
