namespace Grants.ApplicantPortal.API.Web.Configurations;

public static class CorsConfigs
{
  public static IServiceCollection AddCorsConfigs(this IServiceCollection services, WebApplicationBuilder builder, Microsoft.Extensions.Logging.ILogger logger)
  {
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

    logger.LogInformation("CORS configured for frontend applications");
    
    return services;
  }
}
