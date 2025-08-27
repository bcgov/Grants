using System.ComponentModel.DataAnnotations;

namespace Grants.ApplicantPortal.API.Core.Entities;

/// <summary>
/// Audited entity interface
/// </summary>
public interface IAuditedEntity
{
  public DateTime CreatedAt { get; set; }

  [MaxLength(250)]
  public string? CreatedBy { get; set; }

  public DateTime UpdatedAt { get; set; }

  [MaxLength(250)]
  public string? UpdatedBy { get; set; }
}
