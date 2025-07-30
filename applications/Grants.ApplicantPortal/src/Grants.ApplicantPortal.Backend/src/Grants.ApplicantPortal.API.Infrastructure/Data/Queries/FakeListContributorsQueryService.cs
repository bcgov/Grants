using Grants.ApplicantPortal.API.UseCases.Contributors;
using Grants.ApplicantPortal.API.UseCases.Contributors.List;
using Grants.ApplicantPortal.API.Infrastructure.Data;

namespace Grants.ApplicantPortal.API.Infrastructure.Data.Queries;

public class FakeListContributorsQueryService : IListContributorsQueryService
{
  public Task<IEnumerable<ContributorDTO>> ListAsync()
  {
    IEnumerable<ContributorDTO> result =
        [new ContributorDTO(1, SeedData.Contributor1.Name, ""),
        new ContributorDTO(2, SeedData.Contributor2.Name, "")];

    return Task.FromResult(result);
  }
}
