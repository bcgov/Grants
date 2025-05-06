namespace Grants.ApplicantPortal.API.Core.Profiles.ProfileAggregate;

public class Profile(string subject) : EntityBase, IAggregateRoot
{  
  public new Guid Id { get; init; }
  public string Subject { get; private set; } = Guard.Against.NullOrEmpty(subject, nameof(subject));
}
