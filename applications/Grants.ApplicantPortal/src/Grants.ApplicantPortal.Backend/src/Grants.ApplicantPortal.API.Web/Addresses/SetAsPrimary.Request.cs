namespace Grants.ApplicantPortal.API.Web.Addresses;

public class SetAsPrimaryAddressRequest
{
  public const string Route = "/Addresses/{AddressId:Guid}/{ProfileId:Guid}/{PluginId}/{Provider}/set-primary";
  public static string BuildRoute(Guid addressId, Guid profileId, string pluginId, string provider)
    => Route.Replace("{AddressId:Guid}", addressId.ToString())
            .Replace("{ProfileId:Guid}", profileId.ToString())
            .Replace("{PluginId}", pluginId)
            .Replace("{Provider}", provider);

  // Route parameters
  public Guid AddressId { get; set; }
  public Guid ProfileId { get; set; }
  public string PluginId { get; set; } = string.Empty;
  public string Provider { get; set; } = string.Empty;
}
