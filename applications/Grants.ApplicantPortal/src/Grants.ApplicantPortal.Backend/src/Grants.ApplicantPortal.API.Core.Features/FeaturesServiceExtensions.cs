using Microsoft.Extensions.DependencyInjection;

namespace Grants.ApplicantPortal.API.Core.Features;

public static class FeaturesServiceExtensions
{
  public static IServiceCollection AddFeatureServices(
   this IServiceCollection services,
   ILogger logger)
  {

    // Register feature-specific services here
    // Example: services.AddScoped<IYourDomainService, YourDomainService>();

    logger.LogInformation("{Project} services registered", "Features");

    return services;
  }
}
