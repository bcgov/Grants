namespace Grants.ApplicantPortal.API.Web.Contacts;

public class SetAsPrimaryContactRequest
{
  public const string Route = "/Contacts/{ContactId:Guid}/{PluginId}/{Provider}/set-primary";
  public static string BuildRoute(Guid contactId, string pluginId, string provider)
    => Route.Replace("{ContactId:Guid}", contactId.ToString())
            .Replace("{PluginId}", pluginId)
            .Replace("{Provider}", provider);

  public Guid ApplicantId { get; set; }

  // Route parameters
  public Guid ContactId { get; set; }
  public string PluginId { get; set; } = string.Empty;
  public string Provider { get; set; } = string.Empty;  
}
