using Microsoft.Extensions.DependencyInjection;

namespace Grants.ApplicantPortal.API.UseCases;

public static class UseCasesServiceExtensions
{
  public static IServiceCollection AddUseCaseServices(
   this IServiceCollection services,
   ILogger logger)
  {
    // Register shared services
    services.AddScoped<IProfileDataRetrievalService, ProfileDataRetrievalService>();
    services.AddScoped<IProfileCacheInvalidationService, ProfileCacheInvalidationService>();
    services.AddScoped<IPluginCacheService, PluginCacheService>();

    logger.LogInformation("{Project} services registered", "UseCases");

    return services;
  }
}
