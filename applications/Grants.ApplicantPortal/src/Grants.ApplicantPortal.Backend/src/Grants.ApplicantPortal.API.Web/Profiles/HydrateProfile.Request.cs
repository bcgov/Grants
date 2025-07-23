namespace Grants.ApplicantPortal.API.Web.Profiles;

public class HydrateProfileRequest
{
  public const string Route = "/Profiles/{ProfileId:Guid}/{PluginId}/hydrate";
  public static string BuildRoute(Guid profileId, string pluginId)
    => Route.Replace("{ProfileId:Guid}", profileId.ToString())
            .Replace("{PluginId}", pluginId);

  public Guid ProfileId { get; set; }

  /// <summary>
  /// Plugin identifier for profile-specific hydration
  /// </summary>
  public string PluginId { get; set; } = string.Empty;

  /// <summary>
  /// Additional metadata for plugin-specific requests as key-value pairs
  /// </summary>
  public Dictionary<string, object>? AdditionalData { get; set; }
}
