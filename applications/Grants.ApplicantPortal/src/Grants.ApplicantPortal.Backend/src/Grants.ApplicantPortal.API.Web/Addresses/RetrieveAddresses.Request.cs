namespace Grants.ApplicantPortal.API.Web.Addresses;

public class RetrieveAddressesRequest
{
  public const string Route = "/Addresses/{PluginId}/{Provider}";
  public static string BuildRoute(Guid profileId, string pluginId, string provider)
    => Route.Replace("{PluginId}", pluginId)
            .Replace("{Provider}", provider);

  /// <summary>
  /// Plugin identifier for plugin-specific address retrieval
  /// </summary>
  public string PluginId { get; set; } = string.Empty;

  /// <summary>
  /// Provider name provided by the plugin for specific address data retrieval
  /// </summary>
  public string Provider { get; set; } = string.Empty;

  /// <summary>
  /// Additional parameters for plugin-specific requests as key-value pairs
  /// </summary>
  public Dictionary<string, object>? Parameters { get; set; }
}
