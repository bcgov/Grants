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

    var databaseOptions = config.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();

    services.AddDbContext<AppDbContext>((serviceProvider, options) => 
    {
      options.UseNpgsql(connectionString, npgsqlOptions =>
      {
        npgsqlOptions.CommandTimeout(databaseOptions.CommandTimeoutSeconds);
        npgsqlOptions.EnableRetryOnFailure(
          maxRetryCount: databaseOptions.MaxRetryCount,
          maxRetryDelay: TimeSpan.FromSeconds(databaseOptions.MaxRetryDelaySeconds),
          errorCodesToAdd: null);
      });
    });

    services.AddRepositorySupport();
    services.AddQueries();
    services.AddMessagingServices(config, logger);
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
