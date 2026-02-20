using Grants.ApplicantPortal.API.Infrastructure.Data;
using Grants.ApplicantPortal.API.Infrastructure.Messaging;
using Grants.ApplicantPortal.API.Core.Services;
using Grants.ApplicantPortal.API.Infrastructure.Services;

namespace Grants.ApplicantPortal.API.Infrastructure;

public static class InfrastructureServiceExtensions
{
  public static IServiceCollection AddInfrastructureServices(
    this IServiceCollection services,
    ConfigurationManager config,
    ILogger logger)
  {
    string? connectionString = config.GetConnectionString("Grants");
    Guard.Against.Null(connectionString);

    // Add DbContext with PostgreSQL provider
    // The IDomainEventDispatcher dependency will be injected by the Web layer's MediatrConfigs
    services.AddDbContext<AppDbContext>((serviceProvider, options) => 
    {
      options.UseNpgsql(connectionString, npgsqlOptions =>
      {
        // Configure to use the default naming convention (no snake_case conversion)
        // This ensures column names match exactly as defined in migrations
      });
    });
    
    // Register repository support services
    services.AddRepositorySupport();
    
    // Register Custom Queries
    services.AddQueries();

    // Add messaging services (Inbox/Outbox pattern with background jobs)
    services.AddMessagingServices(config, logger);

    // Register core services
    services.AddCoreServices();

    logger.LogInformation("{Project} services registered", "Infrastructure");

    return services;
  }

  /// <summary>
  /// Adds repository support services including generic repositories and unit of work
  /// </summary>
  internal static IServiceCollection AddRepositorySupport(this IServiceCollection services)
  {
    // Register any additional repository support services here
    services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
    services.AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>));
    return services;
  }

  /// <summary>
  /// Adds custom queries
  /// </summary>
  /// <param name="services"></param>
  /// <returns></returns>
  internal static IServiceCollection AddQueries(this IServiceCollection services)
  {
    // Register custom query services here
    // Example: services.AddScoped<IYourQueryService, YourQueryService>();
    return services;
  }

  /// <summary>
  /// Adds core services
  /// </summary>
  internal static IServiceCollection AddCoreServices(this IServiceCollection services)
  {    
    services.AddScoped<IContactManagementService, ContactManagementService>();
    services.AddScoped<IOrganizationManagementService, OrganizationManagementService>();
    services.AddScoped<IAddressManagementService, AddressManagementService>();
    return services;
  }
}
