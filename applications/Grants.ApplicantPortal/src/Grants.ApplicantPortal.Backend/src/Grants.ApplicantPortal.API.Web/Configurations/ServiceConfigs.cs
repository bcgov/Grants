using Grants.ApplicantPortal.API.Core.Email;
using Grants.ApplicantPortal.API.Infrastructure;
using Grants.ApplicantPortal.API.Infrastructure.Email;

namespace Grants.ApplicantPortal.API.Web.Configurations;

public static class ServiceConfigs
{
  public static IServiceCollection AddServiceConfigs(this IServiceCollection services, Microsoft.Extensions.Logging.ILogger logger, WebApplicationBuilder builder)
  {
    services.AddInfrastructureServices(builder.Configuration, logger)
            .AddMediatrConfigs();

    // Add distributed caching services
    // For development, use in-memory cache. For production, consider Redis
    if (builder.Environment.IsDevelopment())
    {
      services.AddDistributedMemoryCache();
    }
    else
    {
      // For production, you would typically use Redis:
      // services.AddStackExchangeRedisCache(options =>
      // {
      //     options.Configuration = builder.Configuration.GetConnectionString("Redis");
      // });
      services.AddDistributedMemoryCache(); // Fallback to memory cache for now
    }

    if (builder.Environment.IsDevelopment())
    {
      // Use a local test email server
      // See: https://ardalis.com/configuring-a-local-test-email-server/
      services.AddScoped<IEmailSender, MimeKitEmailSender>();

      // Otherwise use this:
      //builder.Services.AddScoped<IEmailSender, FakeEmailSender>();

    }
    else
    {
      services.AddScoped<IEmailSender, MimeKitEmailSender>();
    }

    logger.LogInformation("{Project} services registered", "Mediatr, Caching and Email Sender");

    return services;
  }
}
