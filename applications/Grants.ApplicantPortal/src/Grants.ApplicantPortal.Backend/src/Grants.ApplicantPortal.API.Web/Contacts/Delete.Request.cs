namespace Grants.ApplicantPortal.API.Web.Contacts;

public class DeleteContactRequest
{
  public const string Route = "/Contacts/{ContactId:Guid}/{ProfileId:Guid}/{PluginId}/{Provider}";
  public static string BuildRoute(Guid contactId, Guid profileId, string pluginId, string provider)
    => Route.Replace("{ContactId:Guid}", contactId.ToString())
            .Replace("{ProfileId:Guid}", profileId.ToString())
            .Replace("{PluginId}", pluginId)
            .Replace("{Provider}", provider);

  // Route parameters
  public Guid ContactId { get; set; }
  public Guid ProfileId { get; set; }
  public string PluginId { get; set; } = string.Empty;
  public string Provider { get; set; } = string.Empty;
}
