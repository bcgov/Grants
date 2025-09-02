namespace Grants.ApplicantPortal.API.Core.Plugins;

/// <summary>
/// Represents the profile data returned by plugins
/// </summary>
public record ProfileData(
    Guid ProfileId,
    string PluginId,
    string Provider,
    string Key,
    string JsonData,
    DateTime PopulatedAt = default
)
{
  public DateTime PopulatedAt { get; init; } = PopulatedAt == default ? DateTime.UtcNow : PopulatedAt;
}
