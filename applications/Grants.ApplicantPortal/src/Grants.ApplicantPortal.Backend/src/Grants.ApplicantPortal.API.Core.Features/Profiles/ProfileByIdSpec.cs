using Grants.ApplicantPortal.API.Core.Features.Profiles.ProfileAggregate;

namespace Grants.ApplicantPortal.API.Core.Features.Profiles;

/// <summary>
/// Specification for finding a profile by its ID
/// </summary>
public class ProfileByIdSpec : Specification<Profile>
{
  public ProfileByIdSpec(Guid profileId) =>
    Query.Where(profile => profile.Id == profileId);
}
