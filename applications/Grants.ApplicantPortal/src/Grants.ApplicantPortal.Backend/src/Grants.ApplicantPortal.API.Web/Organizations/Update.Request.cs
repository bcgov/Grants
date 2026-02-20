using System.ComponentModel.DataAnnotations;

namespace Grants.ApplicantPortal.API.Web.Organizations;

public class UpdateOrganizationRequest
{
  public const string Route = "/Organizations/{OrganizationId:Guid}/{PluginId}/{Provider}";
  public static string BuildRoute(Guid organizationId, string pluginId, string provider)
    => Route.Replace("{OrganizationId:Guid}", organizationId.ToString())            
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

  [Required]
  public string? NonRegOrgName { get; set; }

  [Required]
  public uint? OrganizationSize { get; set; }

  [Required]
  public string? FiscalMonth { get; set; }

  [Required]
  public int? FiscalDay { get; set; }
  
  // Route parameters
  public Guid OrganizationId { get; set; }
  public string PluginId { get; set; } = string.Empty;
  public string Provider { get; set; } = string.Empty;
}
