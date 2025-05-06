using Grants.ApplicantPortal.API.Core.Profiles.Events;
using Grants.ApplicantPortal.API.Core.Profiles.Interfaces;

namespace Grants.ApplicantPortal.API.Core.Profiles.Services;

/// <summary>
/// This is here mainly so there's an example of a domain service
/// and also to demonstrate how to fire domain events from a service.
/// </summary>
/// <param name="_mediator"></param>
/// <param name="_logger"></param>
public class PopulateProfileService(IMediator _mediator,
  ILogger<PopulateProfileService> _logger) : IPopulateProfileService
{
  public async Task<string> PopulateProfile(Guid profileId, CancellationToken cancellationToken)
  {
    _logger.LogInformation("Populate Profile {profileId}", profileId);
    var domainEvent = new ProfilePopulatedEvent(profileId);
    await _mediator.Publish(domainEvent, cancellationToken);
    return $"Profile Populated: {DateTime.Now}";
  }
}
