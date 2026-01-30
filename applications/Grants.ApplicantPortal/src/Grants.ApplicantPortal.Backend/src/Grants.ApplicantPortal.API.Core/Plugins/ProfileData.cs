namespace Grants.ApplicantPortal.API.Core.Plugins;

/// <summary>
/// Represents the profile data returned by plugins with strongly typed data
/// </summary>
public record ProfileData(
    Guid ProfileId,
    string PluginId,
    string Provider,
    string Key,
    object Data, // ✅ Strongly typed object instead of JSON string
    DateTime PopulatedAt = default
)
{
  public DateTime PopulatedAt { get; init; } = PopulatedAt == default ? DateTime.UtcNow : PopulatedAt;
  
  // ⚠️ DEPRECATED: Keep for backward compatibility during transition
  [Obsolete("Use Data property instead. JsonData will be removed in future version.")]
  public string JsonData => System.Text.Json.JsonSerializer.Serialize(Data, new System.Text.Json.JsonSerializerOptions 
  { 
    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase 
  });
}
