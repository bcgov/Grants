using Grants.ApplicantPortal.API.Infrastructure.Data;
using Grants.ApplicantPortal.API.UseCases.Contributors;
using Grants.ApplicantPortal.API.UseCases.Contributors.List;

namespace Grants.ApplicantPortal.API.Infrastructure.Queries.Contributors;

public class ListContributorsQueryService(AppDbContext _db) : IListContributorsQueryService
{
  // You can use EF, Dapper, SqlClient, etc. for queries -
  // this is just an example

  public async Task<IEnumerable<ContributorDTO>> ListAsync()
  {
    // NOTE: This will fail if testing with EF InMemory provider!
    // Use quoted identifiers to match PostgreSQL's case-sensitive table and column names
    var result = await _db.Database.SqlQuery<ContributorDTO>(
      $"SELECT \"Id\", \"Name\", \"PhoneNumber_Number\" AS \"PhoneNumber\" FROM \"Contributors\"") // don't fetch other big columns
      .ToListAsync();

    return result;
  }
}
