namespace Grants.ApplicantPortal.API.UseCases.Contributors.Get;

public record GetContributorQuery(int ContributorId) : IQuery<Result<ContributorDTO>>;
