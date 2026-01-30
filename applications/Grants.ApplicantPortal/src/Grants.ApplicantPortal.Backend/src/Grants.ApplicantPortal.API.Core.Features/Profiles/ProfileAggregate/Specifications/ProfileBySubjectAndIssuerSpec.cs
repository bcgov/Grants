namespace Grants.ApplicantPortal.API.Core.Features.Profiles.ProfileAggregate.Specifications;

/// <summary>
/// Specification for finding a profile by subject and issuer
/// </summary>
public class ProfileBySubjectAndIssuerSpec : Specification<Profile>
{
    public ProfileBySubjectAndIssuerSpec(string subject, string issuer) =>
        Query.Where(profile => profile.Subject == subject && profile.Issuer == issuer);
}
