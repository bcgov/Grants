using Grants.ApplicantPortal.API.FunctionalTests.ApiEndpoints.Contributors.Mocks;
using Grants.ApplicantPortal.API.Infrastructure.Data;
using Grants.ApplicantPortal.API.UseCases.Contributors.List;
using Microsoft.EntityFrameworkCore;

namespace Grants.ApplicantPortal.API.FunctionalTests;

[Collection("Sequential")]
public class DatabaseConfigurationTest : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public DatabaseConfigurationTest(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public void DatabaseShouldUseInMemoryProvider()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // Verify that we're using in-memory database
        var providerName = context.Database.ProviderName;
        Assert.Equal("Microsoft.EntityFrameworkCore.InMemory", providerName);
    }

    [Fact]
    public void ShouldUseInMemoryQueryService()
    {
        using var scope = _factory.Services.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IListContributorsQueryService>();
        
        // Verify that we're using the in-memory compatible query service
        Assert.IsType<InMemoryListContributorsQueryService>(queryService);
    }

    [Fact]
    public async Task InMemoryQueryServiceShouldWork()
    {
        using var scope = _factory.Services.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IListContributorsQueryService>();
        
        // This should not throw an exception (unlike the raw SQL version)
        var contributors = await queryService.ListAsync();
        
        // Should return the seeded contributors
        Assert.NotNull(contributors);
        Assert.Equal(2, contributors.Count());
    }
}
