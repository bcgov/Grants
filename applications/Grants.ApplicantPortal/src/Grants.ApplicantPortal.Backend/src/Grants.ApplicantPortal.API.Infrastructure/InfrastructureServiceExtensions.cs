using Grants.ApplicantPortal.API.Core.Contributors.Interfaces;
using Grants.ApplicantPortal.API.Core.Contributors.Services;
using Grants.ApplicantPortal.API.Infrastructure.Data;
using Grants.ApplicantPortal.API.Infrastructure.Data.Queries;
using Grants.ApplicantPortal.API.Infrastructure.Plugins;
using Grants.ApplicantPortal.API.Infrastructure.Plugins.Unity;
using Grants.ApplicantPortal.API.Infrastructure.Plugins.Demo;
using Grants.ApplicantPortal.API.UseCases.Contributors.List;
using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.Infrastructure;

public static class InfrastructureServiceExtensions
{
  public static IServiceCollection AddInfrastructureServices(
    this IServiceCollection services,
    ConfigurationManager config,
    ILogger logger)
  {
    string? connectionString = config.GetConnectionString("SqliteConnection");
    Guard.Against.Null(connectionString);
    services.AddDbContext<AppDbContext>(options =>
     options.UseSqlite(connectionString));

    services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>))
           .AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>))
           .AddScoped<IListContributorsQueryService, ListContributorsQueryService>()
           .AddScoped<IDeleteContributorService, DeleteContributorService>();

    // Register profile plugins
    services.AddScoped<IProfilePlugin, UnityProfilePlugin>();
    services.AddScoped<IProfilePlugin, DemoProfilePlugin>();
    services.AddScoped<IProfilePluginFactory, ProfilePluginFactory>();

    logger.LogInformation("{Project} services registered", "Infrastructure");

    return services;
  }
}
