namespace Grants.ApplicantPortal.API.Web.Profiles;

public class RetrieveProfileRequest
{
  public const string Route = "/Profiles/{ProfileId:Guid}/{PluginId}/{Provider}/{Key}";
  public static string BuildRoute(Guid profileId, string pluginId, string provider, string key)
    => Route.Replace("{ProfileId:Guid}", profileId.ToString())
            .Replace("{PluginId}", string.Empty)
            .Replace("{Provider}", string.Empty)
            .Replace("{Key}", string.Empty);

  /// <summary>
  /// The unique identifier for the profile to retrieve
  /// </summary>
  public Guid ProfileId { get; set; }

  /// <summary>
  /// Plugin identifier for plugin-specific profile retrieval
  /// </summary>
  public string PluginId { get; set; } = string.Empty;

  /// <summary>
  /// Provider name provided by the plugin for specific data retrieval
  /// </summary>
  public string Provider { get; set; } = string.Empty;

  /// <summary>
  /// Key for the specific data to retrieve from the plugin provider
  /// </summary>
  public string Key { get; set; } = string.Empty;

  /// <summary>
  /// Additional metadata for plugin-specific requests as key-value pairs
  /// </summary>
  public Dictionary<string, object>? AdditionalData { get; set; }
}
