namespace Grants.ApplicantPortal.API.Web.Plugins;

public class RetrieveProvidersRequest
{
  public const string Route = "/Plugins/{PluginId}/providers";
  public static string BuildRoute(string pluginId)
    => Route.Replace("{PluginId}", pluginId);

  /// <summary>
  /// Plugin identifier to retrieve providers for
  /// </summary>
  public string PluginId { get; set; } = string.Empty;
}
