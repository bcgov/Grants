using System.ComponentModel.DataAnnotations;

namespace Grants.ApplicantPortal.API.Web.Contacts;

public class CreateContactRequest
{
  public const string Route = "/Contacts/{PluginId}/{Provider}";
  public static string BuildRoute(string pluginId, string provider)
    => Route.Replace("{PluginId}", pluginId)
            .Replace("{Provider}", provider);

  [Required]
  public string? Name { get; set; }

  public string? Email { get; set; }
  public string? Title { get; set; }
  
  [Required]
  public string? Role { get; set; }
  
  public string? HomePhoneNumber { get; set; }
  public string? MobilePhoneNumber { get; set; }
  public string? WorkPhoneNumber { get; set; }
  public string? WorkPhoneExtension { get; set; }
  public bool IsPrimary { get; set; }
  public Guid ApplicantId { get; set; }
  
  // Route parameters  
  public string PluginId { get; set; } = string.Empty;
  public string Provider { get; set; } = string.Empty;
}
