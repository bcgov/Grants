using Ardalis.ListStartupServices;
using Grants.ApplicantPortal.API.Infrastructure.Data;
using Grants.ApplicantPortal.API.Plugins;
using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.Web.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

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

    // Add profile resolution middleware after authentication
    app.UseProfileResolution();

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
      var logger = services.GetRequiredService<ILogger<Program>>();
      
      // Get plugin configuration from DI
      var pluginConfigOptions = services.GetService<IOptions<PluginConfiguration>>();
      var pluginConfig = pluginConfigOptions?.Value;
      
      PluginRegistry.Initialize(services, pluginConfig);
      
      var allPluginCount = PluginRegistry.GetAllPluginIds().Count();
      var configuredPluginCount = PluginRegistry.GetConfiguredPlugins(enabledOnly: false).Count();
      var enabledPluginCount = PluginRegistry.GetConfiguredPlugins(enabledOnly: true).Count();
      
      logger.LogInformation("Plugin registry initialized with {AllPluginCount} total plugins, {ConfiguredPluginCount} configured, {EnabledPluginCount} enabled", 
          allPluginCount, configuredPluginCount, enabledPluginCount);
          
      var enabledPlugins = PluginRegistry.GetConfiguredPlugins(enabledOnly: true).Select(p => p.PluginId);
      logger.LogInformation("Enabled plugins: {EnabledPlugins}", string.Join(", ", enabledPlugins));
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
