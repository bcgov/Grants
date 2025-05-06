namespace Grants.ApplicantPortal.API.UseCases.Profiles.Get;

public record GetProfileQuery(Guid ProfileId) : IQuery<Result<ProfileDTO>>;
