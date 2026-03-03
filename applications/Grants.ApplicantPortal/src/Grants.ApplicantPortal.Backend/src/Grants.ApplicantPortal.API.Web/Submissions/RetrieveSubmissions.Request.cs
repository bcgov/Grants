namespace Grants.ApplicantPortal.API.Web.Submissions;

public class RetrieveSubmissionsRequest
{
  public const string Route = "/Submissions/{PluginId}/{Provider}";
  public static string BuildRoute(string pluginId, string provider)
    => Route.Replace("{PluginId}", pluginId)
            .Replace("{Provider}", provider);

  /// <summary>
  /// Plugin identifier for plugin-specific submission retrieval
  /// </summary>
  public string PluginId { get; set; } = string.Empty;

  /// <summary>
  /// Provider name provided by the plugin for specific submission data retrieval
  /// </summary>
  public string Provider { get; set; } = string.Empty;

  /// <summary>
  /// Additional parameters for plugin-specific requests as key-value pairs
  /// </summary>
  public Dictionary<string, object>? Parameters { get; set; }
}
