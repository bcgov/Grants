using System.ComponentModel.DataAnnotations;

namespace Grants.ApplicantPortal.API.Core.Entities;

public interface IAuditedEntity
{
  public DateTime CreatedAt { get; set; }

  public DateTime UpdatedAt { get; set; }

  public DateTime? DeletedAt { get; set; }

  [MaxLength(250)]
  public string? CreatedBy { get; set; }

  [MaxLength(250)]
  public string? UpdatedBy { get; set; }

  [MaxLength(250)]
  public string? DeletedBy { get; set; }

  public bool IsActive { get; set; }

  public bool IsDeleted { get; set; }
}
