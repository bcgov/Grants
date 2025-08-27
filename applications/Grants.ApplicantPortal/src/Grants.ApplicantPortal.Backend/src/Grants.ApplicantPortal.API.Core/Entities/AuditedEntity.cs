using System.ComponentModel.DataAnnotations;

namespace Grants.ApplicantPortal.API.Core.Entities;

/// <summary>
/// Audited entity base class
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="TId"></typeparam>
public abstract class AuditedEntity<TId> : EntityBase<TId>, IAuditedEntity
  where TId : struct, IEquatable<TId>
{
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  [MaxLength(250)]
  public string? CreatedBy { get; set; }

  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

  [MaxLength(250)]
  public string? UpdatedBy { get; set; }
}
