namespace Grants.ApplicantPortal.API.Web.Addresses;

public class RetrieveAddressesRequest
{
  public const string Route = "/Addresses/{ProfileId:Guid}/{PluginId}/{Provider}";
  public static string BuildRoute(Guid profileId, string pluginId, string provider)
    => Route.Replace("{ProfileId:Guid}", profileId.ToString())
            .Replace("{PluginId}", pluginId)
            .Replace("{Provider}", provider);

  /// <summary>
  /// The unique identifier for the profile to retrieve addresses for
  /// </summary>
  public Guid ProfileId { get; set; }

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
