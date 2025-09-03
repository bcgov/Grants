using System.ComponentModel.DataAnnotations;
using Grants.ApplicantPortal.API.Core.Entities;

namespace Grants.ApplicantPortal.API.Core.Features.PluginConfigurations.PluginConfigurationAggregate;

/// <summary>
/// Entity representing plugin configuration stored in database
/// </summary>
public class PluginConfiguration : AuditedEntity<int>, IAggregateRoot
{
  [Required]
  [MaxLength(50)]
  public required string PluginId { get; set; }

  [Required]
  [MaxLength(100)]
  public required string ConfigurationName { get; set; }

  [Required]
  public required string ConfigurationJson { get; set; }
}

