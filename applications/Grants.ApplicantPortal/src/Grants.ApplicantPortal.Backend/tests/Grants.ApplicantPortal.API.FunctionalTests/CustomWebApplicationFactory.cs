using Grants.ApplicantPortal.API.FunctionalTests.ApiEndpoints.Contributors.Mocks;
using Grants.ApplicantPortal.API.Infrastructure.Data;
using Grants.ApplicantPortal.API.UseCases.Contributors.List;
using Microsoft.EntityFrameworkCore;
using Ardalis.SharedKernel;

namespace Grants.ApplicantPortal.API.FunctionalTests;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
  protected override IHost CreateHost(IHostBuilder builder)
  {
    builder.UseEnvironment("Development");
    var host = builder.Build();
    host.Start();

    var serviceProvider = host.Services;

    using (var scope = serviceProvider.CreateScope())
    {
      var scopedServices = scope.ServiceProvider;
      var db = scopedServices.GetRequiredService<AppDbContext>();

      var logger = scopedServices
          .GetRequiredService<ILogger<CustomWebApplicationFactory<TProgram>>>();

      db.Database.EnsureDeleted();
      db.Database.EnsureCreated();

      try
      {
        SeedData.InitializeAsync(db).Wait();
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "An error occurred seeding the " +
                            "database with test messages. Error: {exceptionMessage}", ex.Message);
      }
    }

    return host;
  }

  protected override void ConfigureWebHost(IWebHostBuilder builder)
  {
    builder.ConfigureServices(services =>
    {
      // Remove database-related service registrations
      var descriptorsToRemove = services.Where(d => 
        d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
        d.ServiceType == typeof(DbContextOptions) ||
        d.ServiceType == typeof(AppDbContext)
      ).ToList();

      foreach (var descriptor in descriptorsToRemove)
      {
        services.Remove(descriptor);
      }

      // Remove the existing query service registration that uses raw SQL
      var queryServiceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IListContributorsQueryService));
      if (queryServiceDescriptor != null)
      {
        services.Remove(queryServiceDescriptor);
      }

      string inMemoryCollectionName = Guid.NewGuid().ToString();

      // Create a completely separate service provider for in-memory database like integration tests do
      var inMemoryServiceProvider = new ServiceCollection()
        .AddEntityFrameworkInMemoryDatabase()
        .BuildServiceProvider();

      // Create DbContextOptions manually to avoid conflicts
      var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
      optionsBuilder.UseInMemoryDatabase(inMemoryCollectionName)
                    .UseInternalServiceProvider(inMemoryServiceProvider);
      var options = optionsBuilder.Options;

      // Register the manually created options
      services.AddSingleton(options);

      // Register AppDbContext with a factory that uses the manually created options
      services.AddScoped<AppDbContext>(serviceProvider =>
      {
        var dispatcher = serviceProvider.GetService<IDomainEventDispatcher>();
        return new AppDbContext(options, dispatcher);
      });

      // Register the test-specific query service that works with in-memory database
      services.AddScoped<IListContributorsQueryService, InMemoryListContributorsQueryService>();
    });
  }
}
