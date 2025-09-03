using System.ComponentModel.DataAnnotations;

namespace Grants.ApplicantPortal.API.Core.Entities;

/// <summary>
/// Full audited entity with soft delete
/// </summary>
/// <typeparam name="T"></typeparam>
/// <typeparam name="TId"></typeparam>
public abstract class FullAuditedEntity<TId> : AuditedEntity<TId>, ISoftDeleteEntity
  where TId : struct, IEquatable<TId>
{
  public DateTime? DeletedAt { get; set; }

  [MaxLength(250)]
  public string? DeletedBy { get; set; }

  public bool IsDeleted { get; set; } = false;
}
