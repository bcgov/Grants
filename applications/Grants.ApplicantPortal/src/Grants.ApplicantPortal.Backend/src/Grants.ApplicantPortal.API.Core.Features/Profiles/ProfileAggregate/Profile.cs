using Grants.ApplicantPortal.API.Core.Entities;

namespace Grants.ApplicantPortal.API.Core.Features.Profiles.ProfileAggregate;

/// <summary>
/// Local profile entity representing a user in the system
/// External profile data comes from plugins and is cached separately
/// </summary>
public class Profile : FullAuditedEntity<Guid>, IActiveEntity, IAggregateRoot
{
  /// <summary>
  /// The user's subject identifier (typically from authentication)
  /// </summary>
  public required string Subject { get; set; }

  /// <summary>
  /// The issuer of the user's identity (e.g., the authentication provider)
  /// </summary>
  public required string Issuer { get; set; }

  /// <summary>
  /// Additional metadata about the user
  /// </summary>
  public string? Metadata { get; private set; }

  public bool IsActive { get; set; }

  public Profile()
  {
    // Required properties will be set during object initialization
  }

  public Profile SetSubject(string subject)
  {
    Subject = Guard.Against.NullOrEmpty(subject);
    return this;
  }

  public Profile SetIssuer(string issuer)
  {
    Issuer = Guard.Against.NullOrEmpty(issuer);
    return this;
  }

  public Profile SetMetadata(string metadata)
  {
    Metadata = Guard.Against.NullOrEmpty(metadata);
    return this;
  }
}
