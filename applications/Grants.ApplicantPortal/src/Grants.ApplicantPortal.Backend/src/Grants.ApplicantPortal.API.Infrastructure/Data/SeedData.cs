using Grants.ApplicantPortal.API.Core.Features.Contributors.ContributorAggregate;
using Grants.ApplicantPortal.API.Core.Features.Profiles.ProfileAggregate;

namespace Grants.ApplicantPortal.API.Infrastructure.Data;

public static class SeedData
{
  public static readonly Contributor Contributor1 = new("Ardalis");
  public static readonly Contributor Contributor2 = new("Snowfrog");
  
  private static readonly Guid _testProfileId = Guid.Parse("01985d4b-946c-7dee-90f1-8e2b947ffa83");
  public static readonly Profile Profile1 = new("ABC") { Id = _testProfileId };

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
    var existing = dbContext.Profiles.FirstOrDefault(p => p.Id == _testProfileId);
    if (existing == null)
    { 
      dbContext.Profiles.Add(Profile1);
      await dbContext.SaveChangesAsync();
    }    
  }
}
