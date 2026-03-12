using System.Text.Json;

namespace Grants.ApplicantPortal.API.Core.Plugins;

/// <summary>
/// Metadata extracted from a command message payload.
/// Used by the event system to record failures with full context.
/// </summary>
public record CommandPayloadMetadata(
    string DataType,
    Guid ProfileId,
    string Provider,
    string? EntityId);

/// <summary>
/// Provides plugin-specific metadata for command types: cache segment mapping,
/// friendly names, and payload parsing. Each plugin registers its own provider
/// so the event system stays plugin-agnostic.
/// </summary>
public interface IPluginCommandMetadataProvider
{
  /// <summary>
  /// The plugin ID this provider supplies metadata for.
  /// </summary>
  string PluginId { get; }

  /// <summary>
  /// Returns the cache segment key affected by this command type,
  /// or null if the command type is unknown to this plugin.
  /// </summary>
  string? GetCacheSegment(string dataType);

  /// <summary>
  /// Returns a user-friendly description of the command
  /// (e.g. "contact creation"), or a default if unknown.
  /// </summary>
  string GetFriendlyActionName(string dataType);

  /// <summary>
  /// Extracts context from a JSON command payload.
  /// Returns null if the payload shape is not recognised.
  /// </summary>
  CommandPayloadMetadata? ParsePayload(string jsonPayload);
}

/// <summary>
/// Aggregates all registered <see cref="IPluginCommandMetadataProvider"/> instances
/// and resolves metadata by pluginId. Infrastructure code uses this single registry
/// instead of knowing about individual plugins.
/// </summary>
public interface IPluginCommandMetadataRegistry
{
  string? GetCacheSegment(string pluginId, string dataType);
  string GetFriendlyActionName(string pluginId, string dataType);
  CommandPayloadMetadata? ParsePayload(string pluginId, string jsonPayload);
}

/// <summary>
/// Default implementation that delegates to registered providers.
/// </summary>
public class PluginCommandMetadataRegistry : IPluginCommandMetadataRegistry
{
  private readonly Dictionary<string, IPluginCommandMetadataProvider> _providers;

  public PluginCommandMetadataRegistry(IEnumerable<IPluginCommandMetadataProvider> providers)
  {
    _providers = providers.ToDictionary(p => p.PluginId, p => p, StringComparer.OrdinalIgnoreCase);
  }

  public string? GetCacheSegment(string pluginId, string dataType)
  {
    return _providers.TryGetValue(pluginId, out var provider)
        ? provider.GetCacheSegment(dataType)
        : null;
  }

  public string GetFriendlyActionName(string pluginId, string dataType)
  {
    return _providers.TryGetValue(pluginId, out var provider)
        ? provider.GetFriendlyActionName(dataType)
        : "change";
  }

  public CommandPayloadMetadata? ParsePayload(string pluginId, string jsonPayload)
  {
    return _providers.TryGetValue(pluginId, out var provider)
        ? provider.ParsePayload(jsonPayload)
        : null;
  }
}
