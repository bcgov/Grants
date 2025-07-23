namespace Grants.ApplicantPortal.API.Web.Profiles;

public class RetrieveProfileRequest
{
  public const string Route = "/Profiles/{ProfileId:Guid}/{PluginId}";
  public static string BuildRoute(Guid profileId, string pluginId)
    => Route.Replace("{ProfileId:Guid}", profileId.ToString())
            .Replace("{PluginId}", pluginId);

  public Guid ProfileId { get; set; }

  /// <summary>
  /// Optional plugin identifier for plugin-specific profile retrieval
  /// </summary>
  public string PluginId { get; set; } = string.Empty;

  /// <summary>
  /// Additional metadata for plugin-specific requests as key-value pairs
  /// </summary>
  public Dictionary<string, object>? AdditionalData { get; set; }
}
