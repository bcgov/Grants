using Grants.ApplicantPortal.API.Infrastructure.Data;
using Grants.ApplicantPortal.API.Web.Contributors;


namespace Grants.ApplicantPortal.API.FunctionalTests.ApiEndpoints.Contributors;

[Collection("Sequential")]
public class ContributorGetById(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
  private readonly HttpClient _client = factory.CreateClient();

  [Fact]
  public async Task ReturnsSeedContributorGivenId1()
  {
    var result = await _client.GetAndDeserializeAsync<ContributorRecord>(GetContributorByIdRequest.BuildRoute(1));

    Assert.Equal(1, result.Id);
    Assert.Equal(SeedData.Contributor1.Name, result.Name);
  }

  [Fact]
  public async Task ReturnsNotFoundGivenId1000()
  {
    var route = GetContributorByIdRequest.BuildRoute(1000);
    var response = await _client.GetAndEnsureNotFoundAsync(route);
    
    Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
  }
}
