using System.ComponentModel.DataAnnotations;
using Grants.ApplicantPortal.API.Core.Entities;

namespace Grants.ApplicantPortal.API.Core.Plugins.PluginConfigurations.PluginConfigurationAggregate;

/// <summary>
/// Entity representing plugin configuration stored in database
/// </summary>
public class PluginConfiguration : AuditedEntity
{
  public Guid Id { get; set; } = Guid.NewGuid();

  [Required]
  [MaxLength(50)]
  public required string PluginId { get; set; }

  [Required]
  public required string ConfigurationJson { get; set; }
}

