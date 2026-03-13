namespace Grants.ApplicantPortal.API.Web.Payments;

public class RetrievePaymentsRequest
{
  public const string Route = "/Payments/{PluginId}/{Provider}";
  public static string BuildRoute(string pluginId, string provider)
    => Route.Replace("{PluginId}", pluginId)
            .Replace("{Provider}", provider);

  /// <summary>
  /// Plugin identifier for plugin-specific payment retrieval
  /// </summary>
  public string PluginId { get; set; } = string.Empty;

  /// <summary>
  /// Provider name provided by the plugin for specific payment data retrieval
  /// </summary>
  public string Provider { get; set; } = string.Empty;

  /// <summary>
  /// Additional parameters for plugin-specific requests as key-value pairs
  /// </summary>
  public Dictionary<string, object>? Parameters { get; set; }
}
