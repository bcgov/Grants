using System.ComponentModel.DataAnnotations;

namespace Grants.ApplicantPortal.API.Web.Organizations;

public class UpdateOrganizationRequest
{
  public const string Route = "/Organizations/{OrganizationId:Guid}/{ProfileId:Guid}/{PluginId}/{Provider}";
  public static string BuildRoute(Guid organizationId, Guid profileId, string pluginId, string provider)
    => Route.Replace("{OrganizationId:Guid}", organizationId.ToString())
            .Replace("{ProfileId:Guid}", profileId.ToString())
            .Replace("{PluginId}", pluginId)
            .Replace("{Provider}", provider);

  [Required]
  public string? Name { get; set; }
  
  [Required]
  public string? OrganizationType { get; set; }
  
  [Required]
  public string? OrganizationNumber { get; set; }
  
  [Required]
  public string? Status { get; set; }
  
  public string? LegalName { get; set; }
  public string? DoingBusinessAs { get; set; }
  public string? Ein { get; set; }
  public int? Founded { get; set; }
  public string? FiscalMonth { get; set; }
  public int? FiscalDay { get; set; }
  public string? Mission { get; set; }
  public string[]? ServiceAreas { get; set; }
  
  // Route parameters
  public Guid OrganizationId { get; set; }
  public Guid ProfileId { get; set; }
  public string PluginId { get; set; } = string.Empty;
  public string Provider { get; set; } = string.Empty;
}
