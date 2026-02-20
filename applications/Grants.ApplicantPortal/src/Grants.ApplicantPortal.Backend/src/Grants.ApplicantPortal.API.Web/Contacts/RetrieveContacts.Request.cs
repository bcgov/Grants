namespace Grants.ApplicantPortal.API.Web.Contacts;

public class RetrieveContactsRequest
{
  public const string Route = "/Contacts/{PluginId}/{Provider}";
  public static string BuildRoute(string pluginId, string provider)
    => Route.Replace("{PluginId}", pluginId)
            .Replace("{Provider}", provider);

  /// <summary>
  /// Plugin identifier for plugin-specific contact retrieval
  /// </summary>
  public string PluginId { get; set; } = string.Empty;

  /// <summary>
  /// Provider name provided by the plugin for specific contact data retrieval
  /// </summary>
  public string Provider { get; set; } = string.Empty;

  /// <summary>
  /// Additional parameters for plugin-specific requests as key-value pairs
  /// </summary>
  public Dictionary<string, object>? Parameters { get; set; }
}
