using System.ComponentModel.DataAnnotations;

namespace Grants.ApplicantPortal.API.Core.Entities;

/// <summary>
/// Soft delete entity interface
/// </summary>
public interface ISoftDeleteEntity
{
  public DateTime? DeletedAt { get; set; }

  [MaxLength(250)]
  public string? DeletedBy { get; set; }

  public bool IsDeleted { get; set; }
}
