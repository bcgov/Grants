using Ardalis.SharedKernel;
using Grants.ApplicantPortal.API.Core.Features.Profiles.ProfileAggregate;
using Grants.ApplicantPortal.API.UseCases.Organizations.Retrieve;
using System.Reflection;

namespace Grants.ApplicantPortal.API.Web.Configurations;

public static class MediatrConfigs
{
  public static IServiceCollection AddMediatrConfigs(this IServiceCollection services)
  {
    var mediatRAssemblies = new[]
      {
        Assembly.GetAssembly(typeof(Profile)), // Core.Features
        Assembly.GetAssembly(typeof(RetrieveOrganizationsQueryHandler)), // UseCases
        Assembly.GetAssembly(typeof(Program)) // Web (current assembly)
      };

    services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(mediatRAssemblies!))
            .AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>))
            .AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();

    return services;
  }
}
