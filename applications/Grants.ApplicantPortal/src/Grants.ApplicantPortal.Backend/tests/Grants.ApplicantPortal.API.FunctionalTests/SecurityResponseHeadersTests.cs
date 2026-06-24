namespace Grants.ApplicantPortal.API.FunctionalTests;

public class SecurityResponseHeadersTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
  readonly HttpClient _client;

  public SecurityResponseHeadersTests(CustomWebApplicationFactory<Program> factory)
  {
    _client = factory.CreateClient();
  }

  [Fact]
  public async Task ResponsesIncludeNoCacheHeaders()
  {
    var response = await _client.GetAsync("/healthz");

    Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
    Assert.Equal("no-cache", response.Headers.Pragma.ToString());
  }
}
