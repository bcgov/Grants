using Ardalis.ListStartupServices;
using Grants.ApplicantPortal.API.Infrastructure.Data;
using Grants.ApplicantPortal.API.Infrastructure.Plugins;

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
      // context.Database.Migrate();
      context.Database.EnsureCreated();
      await SeedData.InitializeAsync(context);
    }
    catch (Exception ex)
    {
      var logger = services.GetRequiredService<ILogger<Program>>();
      logger.LogError(ex, "An error occurred seeding the DB. {exceptionMessage}", ex.Message);
    }
  }
}
