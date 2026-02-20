namespace Grants.ApplicantPortal.API.Core.Plugins;

/// <summary>
/// Represents the profile data returned by plugins with strongly typed data
/// </summary>
public record ProfileData(
    Guid ProfileId,
    string PluginId,
    string Provider,
    string Key,
    object Data,
    DateTime PopulatedAt = default
)
{
  public DateTime PopulatedAt { get; init; } = PopulatedAt == default ? DateTime.UtcNow : PopulatedAt;

  /// <summary>
  /// Diagnostic: whether this response came from cache ("HIT") or was freshly fetched ("MISS").
  /// </summary>
  public string? CacheStatus { get; set; }

  /// <summary>
  /// Diagnostic: the backing cache store type ("REDIS" or "MEMORY").
  /// </summary>
  public string? CacheStore { get; set; }
}
