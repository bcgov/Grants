using Grants.ApplicantPortal.API.FunctionalTests.ApiEndpoints.Contributors.Mocks;
using Grants.ApplicantPortal.API.Infrastructure.Data;
using Grants.ApplicantPortal.API.UseCases.Contributors.List;
using Microsoft.EntityFrameworkCore;

namespace Grants.ApplicantPortal.API.FunctionalTests;

/// <summary>
/// Simple test to validate that our in-memory database configuration works
/// without database provider conflicts
/// </summary>
[Collection("Sequential")]
public class InMemoryDatabaseValidationTest : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public InMemoryDatabaseValidationTest(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public void CanCreateServiceScope_WithoutProviderConflicts()
    {
        // This test will fail if there are database provider conflicts
        // because the service provider creation will throw an exception
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        
        // If we get here without an exception, the provider conflict is resolved
        Assert.NotNull(services);
    }

    [Fact]
    public void CanGetAppDbContext_WithInMemoryProvider()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // Verify that we're using in-memory database
        Assert.NotNull(context);
        Assert.Equal("Microsoft.EntityFrameworkCore.InMemory", context.Database.ProviderName);
    }

    [Fact]
    public void CanGetQueryService_WithInMemoryImplementation()
    {
        using var scope = _factory.Services.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IListContributorsQueryService>();
        
        // Verify that we're using the in-memory compatible query service
        Assert.NotNull(queryService);
        Assert.IsType<InMemoryListContributorsQueryService>(queryService);
    }

    [Fact]
    public async Task QueryService_CanExecuteWithoutSqlExceptions()
    {
        using var scope = _factory.Services.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IListContributorsQueryService>();
        
        // This should not throw SQL-related exceptions
        var contributors = await queryService.ListAsync();
        
        // Should return the seeded contributors
        Assert.NotNull(contributors);
        Assert.Equal(2, contributors.Count());
    }
}
