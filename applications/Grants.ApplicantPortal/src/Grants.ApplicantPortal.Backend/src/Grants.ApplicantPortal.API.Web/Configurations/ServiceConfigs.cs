using Grants.ApplicantPortal.API.Core.Email;
using Grants.ApplicantPortal.API.Infrastructure;
using Grants.ApplicantPortal.API.Infrastructure.Email;

namespace Grants.ApplicantPortal.API.Web.Configurations;

public static class ServiceConfigs
{
  public static IServiceCollection AddServiceConfigs(this IServiceCollection services, Microsoft.Extensions.Logging.ILogger logger, WebApplicationBuilder builder)
  {
    services.AddInfrastructureServices(builder.Configuration, logger)
            .AddMediatrConfigs()
            .AddKeycloakAuthentication(builder.Configuration, logger)
            .AddAuthorizationPolicies(logger);

    // Add CORS for frontend applications
    services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            if (builder.Environment.IsDevelopment())
            {
                // Use configured origins or fallback to allow all in development
                var allowedOrigins = builder.Configuration.GetSection("Frontend:AllowedOrigins").Get<string[]>();
                
                if (allowedOrigins?.Length > 0)
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                }
                else
                {
                    // Fallback: Allow all origins in development for easier testing
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                }
            }
            else
            {
                // Production: Restrict to specific frontend origins
                var allowedOrigins = builder.Configuration.GetSection("Frontend:AllowedOrigins").Get<string[]>()
                    ?? new[] { "https://grants-portal.gov.bc.ca" }; // Default production origin
                
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            }
        });
    });

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

    logger.LogInformation("{Project} services registered", "Mediatr, Caching, Email Sender, Authentication, Authorization and CORS");

    return services;
  }
}
