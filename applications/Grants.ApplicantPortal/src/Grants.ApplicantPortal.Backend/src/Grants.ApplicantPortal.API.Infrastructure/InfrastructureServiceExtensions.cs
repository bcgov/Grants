using Grants.ApplicantPortal.API.Infrastructure.Data;
using Grants.ApplicantPortal.API.Infrastructure.Queries.Contributors;
using Grants.ApplicantPortal.API.UseCases.Contributors.List;

namespace Grants.ApplicantPortal.API.Infrastructure;

public static class InfrastructureServiceExtensions
{
  public static IServiceCollection AddInfrastructureServices(
    this IServiceCollection services,
    ConfigurationManager config,
    ILogger logger)
  {
    string? connectionString = config.GetConnectionString("DefaultConnection");
    Guard.Against.Null(connectionString);

    // Add DbContext with PostgreSQL provider
    services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
    // Register repository support services
    services.AddRepositorySupport();
    // Register Custom Queries
    services.AddQueries();

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
    services.AddScoped(typeof(IListContributorsQueryService), typeof(ListContributorsQueryService));
    return services;
  }
}
