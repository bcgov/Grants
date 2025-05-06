namespace Grants.ApplicantPortal.API.Core.Profiles.Interfaces;

// This service and method exist to provide a place in which to fire domain events
// when populating a profile that it is not available in the read model
public interface IPopulateProfileService
{
  public Task<string> PopulateProfile(Guid profileId, CancellationToken cancellationToken);
}
