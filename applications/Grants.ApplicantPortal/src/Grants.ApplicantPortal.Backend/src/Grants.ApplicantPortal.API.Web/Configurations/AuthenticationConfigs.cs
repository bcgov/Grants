using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text.Json;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Grants.ApplicantPortal.API.Web.Configurations;

public static class AuthenticationConfigs
{
    public static IServiceCollection AddKeycloakAuthentication(
        this IServiceCollection services, 
        IConfiguration configuration,
        ILogger logger)
    {
        var keycloakConfig = configuration.GetSection(KeycloakConfiguration.SectionName)
            .Get<KeycloakConfiguration>();

        if (keycloakConfig == null)
        {
            throw new InvalidOperationException("Keycloak configuration is missing");
        }

        var authority = $"{keycloakConfig.AuthServerUrl}/realms/{keycloakConfig.Realm}";
        var metadataAddress = $"{authority}/.well-known/openid-configuration";

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.Authority = authority;
            options.Audience = keycloakConfig.Resource;
            options.RequireHttpsMetadata = keycloakConfig.SslRequired;
            options.MetadataAddress = metadataAddress;
            
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = authority,
                ValidAudience = keycloakConfig.Resource,
                ClockSkew = TimeSpan.FromMinutes(5),
                RoleClaimType = "roles", // Keycloak uses 'roles' claim
                NameClaimType = "preferred_username" // Keycloak uses 'preferred_username' for user name
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    logger.LogError("Authentication failed: {Error}", context.Exception?.Message);
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    logger.LogDebug("Token validated for user: {User}", 
                        context.Principal?.Identity?.Name ?? "Unknown");
                    
                    // Transform Keycloak roles into standard claims
                    TransformKeycloakClaims(context.Principal, logger);
                    
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    logger.LogWarning("Authentication challenge: {Error}", context.Error);
                    return Task.CompletedTask;
                }
            };
        });

        logger.LogInformation("Keycloak authentication configured for authority: {Authority}", authority);

        return services;
    }

    /// <summary>
    /// Transform Keycloak JWT claims into standard ASP.NET Core claims
    /// </summary>
    private static void TransformKeycloakClaims(ClaimsPrincipal? principal, ILogger logger)
    {
        if (principal?.Identity is not ClaimsIdentity identity)
            return;

        try
        {
            // Extract realm roles from realm_access claim
            var realmAccessClaim = identity.FindFirst("realm_access");
            if (realmAccessClaim != null)
            {
                var realmAccess = JsonSerializer.Deserialize<JsonElement>(realmAccessClaim.Value);
                if (realmAccess.TryGetProperty("roles", out var rolesElement) && rolesElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var role in rolesElement.EnumerateArray())
                    {
                        if (role.ValueKind == JsonValueKind.String)
                        {
                            identity.AddClaim(new Claim(ClaimTypes.Role, role.GetString()!));
                            identity.AddClaim(new Claim("roles", role.GetString()!));
                        }
                    }
                }
            }

            // Extract resource roles if needed
            var resourceAccessClaim = identity.FindFirst("resource_access");
            if (resourceAccessClaim != null)
            {
                var resourceAccess = JsonSerializer.Deserialize<JsonElement>(resourceAccessClaim.Value);
                foreach (var resource in resourceAccess.EnumerateObject())
                {
                    if (resource.Value.TryGetProperty("roles", out var resourceRoles) && 
                        resourceRoles.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var role in resourceRoles.EnumerateArray())
                        {
                            if (role.ValueKind == JsonValueKind.String)
                            {
                                identity.AddClaim(new Claim("resource_role", $"{resource.Name}:{role.GetString()}"));
                            }
                        }
                    }
                }
            }

            logger.LogDebug("Transformed Keycloak claims for user: {User}", principal.Identity.Name);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to transform Keycloak claims");
        }
    }
}
