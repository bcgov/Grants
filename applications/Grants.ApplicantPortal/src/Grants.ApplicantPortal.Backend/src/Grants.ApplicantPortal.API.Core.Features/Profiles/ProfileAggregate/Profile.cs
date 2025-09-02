using Grants.ApplicantPortal.API.Core.Entities;

namespace Grants.ApplicantPortal.API.Core.Features.Profiles.ProfileAggregate;

/// <summary>
/// Local profile entity representing a user in the system
/// External profile data comes from plugins and is cached separately
/// </summary>
public class Profile(string subject) : FullAuditedEntity<Guid>, IActiveEntity, IAggregateRoot
{
  /// <summary>
  /// The user's subject identifier (typically from authentication)
  /// </summary>
  public string Subject { get; private set; } = Guard.Against.NullOrEmpty(subject, nameof(subject));
  public bool IsActive { get; set; }
}
