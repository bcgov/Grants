using System.ComponentModel.DataAnnotations;

namespace Grants.ApplicantPortal.API.Web.Contacts;

public class CreateContactRequest
{
  public const string Route = "/Contacts/{ProfileId:Guid}/{PluginId}/{Provider}";
  public static string BuildRoute(Guid profileId, string pluginId, string provider)
    => Route.Replace("{ProfileId:Guid}", profileId.ToString())
            .Replace("{PluginId}", pluginId)
            .Replace("{Provider}", provider);

  [Required]
  public string? Name { get; set; }

  public string? Email { get; set; }
  public string? Title { get; set; }
  
  [Required]
  public string? Type { get; set; }
  
  public string? PhoneNumber { get; set; }
  public bool IsPrimary { get; set; }
  
  // Route parameters
  public Guid ProfileId { get; set; }
  public string PluginId { get; set; } = string.Empty;
  public string Provider { get; set; } = string.Empty;
}
