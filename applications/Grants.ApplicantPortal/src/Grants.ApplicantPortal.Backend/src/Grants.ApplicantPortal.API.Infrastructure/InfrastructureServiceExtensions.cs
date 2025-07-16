using Grants.ApplicantPortal.API.Core.Contributors.Interfaces;
using Grants.ApplicantPortal.API.Core.Contributors.Services;
using Grants.ApplicantPortal.API.Core.Profiles.Interfaces;
using Grants.ApplicantPortal.API.Core.Profiles.Services;
using Grants.ApplicantPortal.API.Infrastructure.Data;
using Grants.ApplicantPortal.API.Infrastructure.Data.Queries;
using Grants.ApplicantPortal.API.UseCases.Contributors.List;

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
           .AddScoped<IDeleteContributorService, DeleteContributorService>()
           .AddScoped<IPopulateProfileService, PopulateProfileService>();


    logger.LogInformation("{Project} services registered", "Infrastructure");

    return services;
  }
}
