namespace Grants.ApplicantPortal.API.Web.Addresses;

public class RetrieveAddressTypesRequest
{
  public const string Route = "/Addresses/{PluginId}/types";
  public static string BuildRoute(string pluginId)
    => Route.Replace("{PluginId}", pluginId);

  /// <summary>
  /// Plugin identifier to retrieve address types for
  /// </summary>
  public string PluginId { get; set; } = string.Empty;
}
