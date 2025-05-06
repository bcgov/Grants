namespace Grants.ApplicantPortal.API.Core.Profiles.Events;

/// <summary>
/// A domain event that is dispatched whenever a profile is populated.
/// The PopulateProfileService is used to dispatch this event.
/// </summary>
internal sealed class ProfilePopulatedEvent(Guid profileId) : DomainEventBase
{
  public Guid ProfileId { get; init; } = profileId;
}

