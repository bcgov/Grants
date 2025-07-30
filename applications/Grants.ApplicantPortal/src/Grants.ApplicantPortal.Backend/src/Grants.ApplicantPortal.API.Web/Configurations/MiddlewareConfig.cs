using Ardalis.ListStartupServices;
using Grants.ApplicantPortal.API.Infrastructure.Data;
using Grants.ApplicantPortal.API.Infrastructure.Plugins;
using Microsoft.EntityFrameworkCore;

namespace Grants.ApplicantPortal.API.Web.Configurations;

public static class MiddlewareConfig
{
  public static async Task<IApplicationBuilder> UseAppMiddlewareAndSeedDatabase(this WebApplication app)
  {
    if (app.Environment.IsDevelopment())
    {
      app.UseDeveloperExceptionPage();
      app.UseShowAllServicesMiddleware(); // see https://github.com/ardalis/AspNetCoreStartupServices
    }
    else
    {
      app.UseDefaultExceptionHandler(); // from FastEndpoints
      app.UseHsts();
    }

    app.UseFastEndpoints()
        .UseSwaggerGen(); // Includes AddFileServer and static files middleware

    app.UseHttpsRedirection(); // Note this will drop Authorization headers

    // Add CORS before authentication
    app.UseCors("AllowFrontend");

    // Add authentication and authorization middleware
    app.UseAuthentication();
    app.UseAuthorization();

    // Initialize plugin registry at startup
    InitializePluginRegistry(app);

    await SeedDatabase(app);

    return app;
  }

  static void InitializePluginRegistry(WebApplication app)
  {
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;

    try
    {
      PluginRegistry.Initialize(services);
      var logger = services.GetRequiredService<ILogger<Program>>();
      var pluginCount = PluginRegistry.GetAllPluginIds().Count();
      logger.LogInformation("Plugin registry initialized with {PluginCount} plugins: {PluginIds}", 
          pluginCount, string.Join(", ", PluginRegistry.GetAllPluginIds()));
    }
    catch (Exception ex)
    {
      var logger = services.GetRequiredService<ILogger<Program>>();
      logger.LogError(ex, "An error occurred initializing the plugin registry. {exceptionMessage}", ex.Message);
    }
  }

  static async Task SeedDatabase(WebApplication app)
  {
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;

    try
    {
      var context = services.GetRequiredService<AppDbContext>();
      var configuration = services.GetRequiredService<IConfiguration>();
      var logger = services.GetRequiredService<ILogger<Program>>();

      // Get database configuration options
      var runMigrationsOnStartup = configuration.GetValue<bool>("Database:RunMigrationsOnStartup", false);
      var seedDataOnStartup = configuration.GetValue<bool>("Database:SeedDataOnStartup", false);

      logger.LogInformation("Database initialization settings - RunMigrations: {RunMigrations}, SeedData: {SeedData}", 
          runMigrationsOnStartup, seedDataOnStartup);

      if (runMigrationsOnStartup)
      {
        logger.LogInformation("Running database migrations...");
        await context.Database.MigrateAsync();
        logger.LogInformation("Database migrations completed successfully");
      }
      else
      {
        logger.LogInformation("Skipping database migrations (disabled in configuration)");
        // For development/testing scenarios, ensure database exists
        if (app.Environment.IsDevelopment())
        {
          context.Database.EnsureCreated();
        }
      }

      if (seedDataOnStartup)
      {
        logger.LogInformation("Running database seeding...");
        await SeedData.InitializeAsync(context);
        logger.LogInformation("Database seeding completed successfully");
      }
      else
      {
        logger.LogInformation("Skipping database seeding (disabled in configuration)");
      }
    }
    catch (Exception ex)
    {
      var logger = services.GetRequiredService<ILogger<Program>>();
      logger.LogError(ex, "An error occurred during database initialization. {exceptionMessage}", ex.Message);
    }
  }
}
