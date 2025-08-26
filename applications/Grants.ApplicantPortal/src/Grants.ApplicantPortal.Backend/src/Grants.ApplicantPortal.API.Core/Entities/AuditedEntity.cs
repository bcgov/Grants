using System.ComponentModel.DataAnnotations;

namespace Grants.ApplicantPortal.API.Core.Entities;

public class AuditedEntity : EntityBase, IAuditedEntity
{
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

  public DateTime? DeletedAt { get; set; }

  [MaxLength(250)]
  public string? CreatedBy { get; set; }

  [MaxLength(250)]
  public string? UpdatedBy { get; set; }

  [MaxLength(250)]
  public string? DeletedBy { get; set; }

  public bool IsActive { get; set; } = true;

  public bool IsDeleted { get; set; } = false;
}
