namespace Grants.ApplicantPortal.API.Web.Contacts;

public class RetrieveContactRolesRequest
{
  public const string Route = "/Contacts/{PluginId}/roles";
  public static string BuildRoute(string pluginId)
    => Route.Replace("{PluginId}", pluginId);

  /// <summary>
  /// Plugin identifier to retrieve contact roles for
  /// </summary>
  public string PluginId { get; set; } = string.Empty;
}
