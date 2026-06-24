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
    using var response = await _client.GetAsync("/healthz");

    response.EnsureSuccessStatusCode();

    Assert.NotNull(response.Headers.CacheControl);
    Assert.True(response.Headers.CacheControl!.NoStore);
    Assert.Contains("no-cache", response.Headers.Pragma.ToString());
  }

  [Fact]
  public async Task ResponsesDoNotIncludeServerHeader()
  {
    using var response = await _client.GetAsync("/healthz");

    response.EnsureSuccessStatusCode();

    Assert.False(response.Headers.Contains("Server"));
  }
}
