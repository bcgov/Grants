namespace Grants.ApplicantPortal.API.Web.Addresses;

public class DeleteAddressRequest
{
  public const string Route = "/Addresses/{AddressId:Guid}/{PluginId}/{Provider}";
  public static string BuildRoute(Guid addressId, string pluginId, string provider)
    => Route.Replace("{AddressId:Guid}", addressId.ToString())
            .Replace("{PluginId}", pluginId)
            .Replace("{Provider}", provider);

  // Route parameters
  public Guid AddressId { get; set; }
  public Guid ApplicantId { get; set; }

  public string PluginId { get; set; } = string.Empty;
  public string Provider { get; set; } = string.Empty;
}
