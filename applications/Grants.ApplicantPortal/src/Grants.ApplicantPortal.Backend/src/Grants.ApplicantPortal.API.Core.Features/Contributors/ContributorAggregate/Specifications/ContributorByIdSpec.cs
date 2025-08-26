using Grants.ApplicantPortal.API.Core.Features.Contributors.ContributorAggregate;

namespace Grants.ApplicantPortal.API.Core.Features.Contributors.ContributorAggregate.Specifications;

public class ContributorByIdSpec : Specification<Contributor>
{
  public ContributorByIdSpec(int contributorId) =>
    Query
        .Where(contributor => contributor.Id == contributorId);
}
