using Grants.ApplicantPortal.API.Infrastructure.Data;
using Grants.ApplicantPortal.API.Infrastructure.Data.Queries;
using Grants.ApplicantPortal.API.UseCases.Contributors.List;

namespace Grants.ApplicantPortal.API.FunctionalTests;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
  /// <summary>
  /// Overriding CreateHost to avoid creating a separate ServiceProvider per this thread:
  /// https://github.com/dotnet-architecture/eShopOnWeb/issues/465
  /// </summary>
  /// <param name="builder"></param>
  /// <returns></returns>
  protected override IHost CreateHost(IHostBuilder builder)
  {
    builder.UseEnvironment("Development"); // will not send real emails
    var host = builder.Build();
    host.Start();

    // Get service provider.
    var serviceProvider = host.Services;

    // Create a scope to obtain a reference to the database
    // context (AppDbContext).
    using (var scope = serviceProvider.CreateScope())
    {
      var scopedServices = scope.ServiceProvider;
      var db = scopedServices.GetRequiredService<AppDbContext>();

      var logger = scopedServices
          .GetRequiredService<ILogger<CustomWebApplicationFactory<TProgram>>>();

      // Reset Sqlite database for each test run
      // If using a real database, you'll likely want to remove this step.
      db.Database.EnsureDeleted();

      // Ensure the database is created.
      db.Database.EnsureCreated();

      try
      {
        // Can also skip creating the items
        //if (!db.ToDoItems.Any())
        //{
        // Seed the database with test data.
        SeedData.InitializeAsync(db).Wait();
        //}
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
    builder
        .ConfigureServices(services =>
        {
          // Configure test dependencies here
          
          // Replace the ListContributorsQueryService with the fake implementation
          // The real implementation uses raw SQL which doesn't work with EF InMemory provider
          var descriptor = services.SingleOrDefault(
              d => d.ServiceType == typeof(IListContributorsQueryService));
          
          if (descriptor != null)
          {
            services.Remove(descriptor);
          }
          
          services.AddScoped<IListContributorsQueryService, FakeListContributorsQueryService>();

          //// Remove the app's ApplicationDbContext registration.
          //var descriptor = services.SingleOrDefault(
          //d => d.ServiceType ==
          //    typeof(DbContextOptions<AppDbContext>));

          //if (descriptor != null)
          //{
          //  services.Remove(descriptor);
          //}

          //// This should be set for each individual test run
          //string inMemoryCollectionName = Guid.NewGuid().ToString();

          //// Add ApplicationDbContext using an in-memory database for testing.
          //services.AddDbContext<AppDbContext>(options =>
          //{
          //  options.UseInMemoryDatabase(inMemoryCollectionName);
          //});
        });
  }
}
