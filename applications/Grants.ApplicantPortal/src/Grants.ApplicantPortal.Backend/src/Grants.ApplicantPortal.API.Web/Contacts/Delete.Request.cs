namespace Grants.ApplicantPortal.API.Web.Contacts;

public class DeleteContactRequest
{
  public const string Route = "/Contacts/{ContactId:Guid}/{PluginId}/{Provider}";
  public static string BuildRoute(Guid contactId, string pluginId, string provider)
    => Route.Replace("{ContactId:Guid}", contactId.ToString())
            .Replace("{PluginId}", pluginId)
            .Replace("{Provider}", provider);

  // Route parameters
  public Guid ContactId { get; set; }
  public Guid ApplicantId { get; set; }

  public string PluginId { get; set; } = string.Empty;
  public string Provider { get; set; } = string.Empty;
}
