namespace Grants.ApplicantPortal.API.Core.Plugins;

/// <summary>
/// Represents the metadata payload for profile population requests
/// </summary>
public record ProfilePopulationMetadata(
    Guid ProfileId,
    string PluginId,
    string Provider,
    string Key,
    Dictionary<string, object>? AdditionalData = null,
    DateTime RequestedAt = default
)
{
  public DateTime RequestedAt { get; init; } = RequestedAt == default ? DateTime.UtcNow : RequestedAt;
}
