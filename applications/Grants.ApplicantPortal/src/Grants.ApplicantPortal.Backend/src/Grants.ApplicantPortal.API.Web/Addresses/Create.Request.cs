using System.ComponentModel.DataAnnotations;

namespace Grants.ApplicantPortal.API.Web.Addresses;

public class CreateAddressRequest
{
  public const string Route = "/Addresses/{PluginId}/{Provider}";
  public static string BuildRoute(string pluginId, string provider)
    => Route.Replace("{PluginId}", pluginId)
            .Replace("{Provider}", provider);

  [Required]
  public string? AddressType { get; set; }

  [Required]
  public string? Street { get; set; }

  public string? Street2 { get; set; }

  public string? Unit { get; set; }

  [Required]
  public string? City { get; set; }

  [Required]
  public string? Province { get; set; }

  [Required]
  public string? PostalCode { get; set; }

  public string? Country { get; set; }

  public bool IsPrimary { get; set; }

  public Guid ApplicantId { get; set; }

  // Route parameters
  public string PluginId { get; set; } = string.Empty;
  public string Provider { get; set; } = string.Empty;
}
