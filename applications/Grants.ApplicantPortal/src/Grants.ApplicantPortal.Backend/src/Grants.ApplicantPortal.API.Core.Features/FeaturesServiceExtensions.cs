using Grants.ApplicantPortal.API.Core.Features.Contributors.Interfaces;
using Grants.ApplicantPortal.API.Core.Features.Contributors.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grants.ApplicantPortal.API.Core.Features;

public static class FeaturesServiceExtensions
{
  public static IServiceCollection AddFeatureServices(
   this IServiceCollection services,
   ILogger logger)
  {

    // Register feature-specific services
    services.AddScoped<IDeleteContributorService, DeleteContributorService>();

    logger.LogInformation("{Project} services registered", "Features");

    return services;
  }
}
