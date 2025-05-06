using Grants.ApplicantPortal.API.Core.Contributors.ContributorAggregate;
using Grants.ApplicantPortal.API.Core.Profiles.ProfileAggregate;

namespace Grants.ApplicantPortal.API.Infrastructure.Data;

public static class SeedData
{
  public static readonly Contributor Contributor1 = new("Ardalis");
  public static readonly Contributor Contributor2 = new("Snowfrog");

  public static readonly Profile Profile1 = new("ABC");

  public static async Task InitializeAsync(AppDbContext dbContext)
  {
    // Check flag 
    if (!await dbContext.Contributors.AnyAsync())
    {
      await PopulateTestContributors(dbContext);
    }

    if (!await dbContext.Profiles.AnyAsync())
    {
      await PopulateTestProfiles(dbContext);
    }
  }

  public static async Task PopulateTestContributors(AppDbContext dbContext)
  {
    dbContext.Contributors.Add(Contributor1);
    dbContext.Contributors.Add(Contributor2);
    await dbContext.SaveChangesAsync();
  }

  public static async Task PopulateTestProfiles(AppDbContext dbContext)
  {
    dbContext.Profiles.Add(Profile1);
    await dbContext.SaveChangesAsync();
  }
}
