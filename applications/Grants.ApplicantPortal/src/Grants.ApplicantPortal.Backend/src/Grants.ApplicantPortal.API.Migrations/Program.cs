using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Grants.ApplicantPortal.API.Infrastructure.Data;
using Ardalis.SharedKernel;
using Serilog;

namespace Grants.ApplicantPortal.API.Migrations;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Configure Serilog - console output is already configured in appsettings.json
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .CreateLogger();

        try
        {
            var host = CreateHostBuilder(args, configuration).Build();

            using var scope = host.Services.CreateScope();
            var services = scope.ServiceProvider;
            
            // Get logger from DI container - this will be the only logger used
            var logger = services.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("Starting database migration and seeding process");

            var context = services.GetRequiredService<AppDbContext>();

            logger.LogInformation("Applying database migrations...");
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations completed successfully");

            logger.LogInformation("Starting database seeding...");
            await SeedData.InitializeAsync(context);
            logger.LogInformation("Database seeding completed successfully");

            logger.LogInformation("Migration and seeding process completed successfully");
            return 0;
        }
        catch (Exception ex)
        {
            // Use Serilog for fatal errors since DI container might not be available
            Log.Fatal(ex, "Migration and seeding process failed");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args, IConfiguration configuration) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                // Clear all default logging providers
                logging.ClearProviders();
            })
            .UseSerilog() // Use only Serilog for logging
            .ConfigureServices((hostContext, services) =>
            {
                // Get connection string from configuration
                string? connectionString = configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("DefaultConnection string is not configured.");
                }

                // Add MediatR for domain event dispatching
                services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
                
                // Add the domain event dispatcher required by AppDbContext
                services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();

                // Add DbContext with PostgreSQL
                services.AddDbContext<AppDbContext>(options =>
                    options.UseNpgsql(connectionString));
            });
}
