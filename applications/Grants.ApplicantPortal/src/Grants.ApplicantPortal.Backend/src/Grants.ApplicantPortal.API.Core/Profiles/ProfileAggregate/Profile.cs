namespace Grants.ApplicantPortal.API.Core.Profiles.ProfileAggregate;

/// <summary>
/// Local profile entity representing a user in the system
/// External profile data comes from plugins and is cached separately
/// </summary>
public class Profile(string subject) : EntityBase, IAggregateRoot
{  
  public new Guid Id { get; init; }
  
  /// <summary>
  /// The user's subject identifier (typically from authentication)
  /// </summary>
  public string Subject { get; private set; } = Guard.Against.NullOrEmpty(subject, nameof(subject));
}
