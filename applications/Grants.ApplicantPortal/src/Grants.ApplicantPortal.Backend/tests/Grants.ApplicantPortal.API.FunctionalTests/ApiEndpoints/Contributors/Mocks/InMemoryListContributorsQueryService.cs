using Grants.ApplicantPortal.API.Infrastructure.Data;
using Grants.ApplicantPortal.API.UseCases.Contributors;
using Grants.ApplicantPortal.API.UseCases.Contributors.List;
using Microsoft.EntityFrameworkCore;

namespace Grants.ApplicantPortal.API.FunctionalTests.ApiEndpoints.Contributors.Mocks;

/// <summary>
/// In-memory database compatible implementation of IListContributorsQueryService
/// This avoids the raw SQL queries that don't work with the in-memory provider
/// </summary>
public class InMemoryListContributorsQueryService(AppDbContext db) : IListContributorsQueryService
{
  public async Task<IEnumerable<ContributorDTO>> ListAsync()
  {
    // Use LINQ instead of raw SQL to work with in-memory database
    var result = await db.Contributors
      .Select(c => new ContributorDTO(
        c.Id,
        c.Name,
        c.PhoneNumber != null ? c.PhoneNumber.Number : null))
      .ToListAsync();

    return result;
  }
}
