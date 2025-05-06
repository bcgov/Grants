using Grants.ApplicantPortal.API.Core.Profiles.ProfileAggregate;

namespace Grants.ApplicantPortal.API.Core.Profiles;
public class ProfileByIdSpec : Specification<Profile>
{
  public ProfileByIdSpec(Guid profileId) =>
    Query
        .Where(profile => profile.Id == profileId);
}
