using System.ComponentModel.DataAnnotations;

namespace Grants.ApplicantPortal.API.Web.Addresses;

public class UpdateAddressRequest
{
  public const string Route = "/Addresses/{AddressId:Guid}/{ProfileId:Guid}/{PluginId}/{Provider}";
  public static string BuildRoute(Guid addressId, Guid profileId, string pluginId, string provider)
    => Route.Replace("{AddressId:Guid}", addressId.ToString())
            .Replace("{ProfileId:Guid}", profileId.ToString())
            .Replace("{PluginId}", pluginId)
            .Replace("{Provider}", provider);

  [Required]
  public string? Type { get; set; }
  
  [Required]
  public string? Address { get; set; }
  
  [Required]
  public string? City { get; set; }
  
  [Required]
  public string? Province { get; set; }
  
  [Required]
  public string? PostalCode { get; set; }
  
  public string? Country { get; set; }
  
  public bool IsPrimary { get; set; }
  
  // Route parameters
  public Guid AddressId { get; set; }
  public Guid ProfileId { get; set; }
  public string PluginId { get; set; } = string.Empty;
  public string Provider { get; set; } = string.Empty;
}
