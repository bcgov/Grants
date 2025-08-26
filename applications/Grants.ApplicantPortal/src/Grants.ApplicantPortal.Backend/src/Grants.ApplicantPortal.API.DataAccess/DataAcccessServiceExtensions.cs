using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.Infrastructure.Data.Queries.Contributors;
using Grants.ApplicantPortal.API.Infrastructure.Data.Repositories;
using Grants.ApplicantPortal.API.UseCases.Contributors.List;
namespace Grants.ApplicantPortal.API.DataAccess;

public static class DataAcccessServiceExtensions
{
  public static IServiceCollection AddDataAcccessServices(
    this IServiceCollection services,
    ILogger logger)
  {
    // Register query and repository services
    services.AddScoped<IPluginConfigurationRepository, PluginConfigurationRepository>();
    services.AddScoped<IListContributorsQueryService, ListContributorsQueryService>();

    logger.LogInformation("{Project} services registered", "DataAccess");

    return services;
  }
}
