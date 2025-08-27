using Microsoft.AspNetCore.Authorization;
using Grants.ApplicantPortal.API.Web.Auth;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Grants.ApplicantPortal.API.Web.Configurations;

public static class AuthorizationConfigs
{
    public static IServiceCollection AddAuthorizationPolicies(
        this IServiceCollection services,
        ILogger logger)
    {
        services.AddAuthorization(options =>
        {
            // Configure default authorization policy
            ConfigureBasicPolicies(options);
            
            // Configure role-based policies
            ConfigureRolePolicies(options);
            
            // Configure resource-based policies
            ConfigureResourcePolicies(options);
            
            // Configure custom business logic policies
            ConfigureBusinessPolicies(options);
        });

        logger.LogInformation("Authorization policies configured");
        return services;
    }

    /// <summary>
    /// Configure basic authentication policies
    /// </summary>
    private static void ConfigureBasicPolicies(AuthorizationOptions options)
    {
        // Default policy requiring authenticated user
        options.AddPolicy(AuthPolicies.RequireAuthenticatedUser, policy =>
        {
            policy.RequireAuthenticatedUser();
        });

        // Policy requiring realm roles from Keycloak
        options.AddPolicy(AuthPolicies.RequireRealmRole, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context =>
            {
                // Check if user has any realm roles
                return context.User.Claims.Any(c => c.Type == "realm_access") ||
                       context.User.Claims.Any(c => c.Type == "roles");
            });
        });
    }

    /// <summary>
    /// Configure role-based authorization policies
    /// </summary>
    private static void ConfigureRolePolicies(AuthorizationOptions options)
    {
        // Admin only access
        options.AddPolicy(AuthPolicies.AdminOnly, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireRole("admin");
        });

        // User or Admin access
        options.AddPolicy(AuthPolicies.UserOrAdmin, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context =>
                context.User.IsInRole("user") || context.User.IsInRole("admin"));
        });

        // System Administrator - highest level access
        options.AddPolicy(AuthPolicies.SystemAdmin, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireRole("system-admin");
        });

        // Program Manager - can manage grants programs
        options.AddPolicy(AuthPolicies.ProgramManager, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context =>
                context.User.IsInRole("program-manager") || 
                context.User.IsInRole("admin") ||
                context.User.IsInRole("system-admin"));
        });

        // Grant Officer - can review and process applications
        options.AddPolicy(AuthPolicies.GrantOfficer, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context =>
                context.User.IsInRole("grant-officer") ||
                context.User.IsInRole("program-manager") ||
                context.User.IsInRole("admin") ||
                context.User.IsInRole("system-admin"));
        });
    }

    /// <summary>
    /// Configure resource-based authorization policies
    /// </summary>
    private static void ConfigureResourcePolicies(AuthorizationOptions options)
    {
        // Profile management policies
        options.AddPolicy(AuthPolicies.CanReadProfiles, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context =>
                context.User.IsInRole("user") ||
                context.User.IsInRole("grant-officer") ||
                context.User.IsInRole("program-manager") ||
                context.User.IsInRole("admin"));
        });

        options.AddPolicy(AuthPolicies.CanManageProfiles, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context =>
                context.User.IsInRole("grant-officer") ||
                context.User.IsInRole("program-manager") ||
                context.User.IsInRole("admin"));
        });

        // Application management policies
        options.AddPolicy(AuthPolicies.CanSubmitApplications, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireRole("user");
        });

        options.AddPolicy(AuthPolicies.CanReviewApplications, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context =>
                context.User.IsInRole("grant-officer") ||
                context.User.IsInRole("program-manager") ||
                context.User.IsInRole("admin"));
        });

        // System management policies
        options.AddPolicy(AuthPolicies.CanManageSystem, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context =>
                context.User.IsInRole("admin") ||
                context.User.IsInRole("system-admin"));
        });
    }

    /// <summary>
    /// Configure business logic-based authorization policies
    /// </summary>
    private static void ConfigureBusinessPolicies(AuthorizationOptions options)
    {
        // Email verification requirement
        options.AddPolicy(AuthPolicies.RequireVerifiedEmail, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context =>
            {
                var emailVerified = context.User.FindFirst("email_verified")?.Value;
                return emailVerified == "true";
            });
        });

        // Profile completion requirement
        options.AddPolicy(AuthPolicies.RequireCompleteProfile, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context =>
            {
                // Check if user has completed their profile
                var hasGivenName = !string.IsNullOrEmpty(context.User.FindFirst("given_name")?.Value);
                var hasFamilyName = !string.IsNullOrEmpty(context.User.FindFirst("family_name")?.Value);
                var hasEmail = !string.IsNullOrEmpty(context.User.FindFirst("email")?.Value);
                
                return hasGivenName && hasFamilyName && hasEmail;
            });
        });

        // Terms of service acceptance
        options.AddPolicy(AuthPolicies.RequireTermsAcceptance, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context =>
            {
                // Check if user has accepted current terms of service
                // This would typically check against a database or specific claim
                var termsAccepted = context.User.FindFirst("terms_accepted")?.Value;
                return termsAccepted == "true";
            });
        });

        // Combined business requirements
        options.AddPolicy(AuthPolicies.FullyVerifiedUser, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context =>
            {
                var emailVerified = context.User.FindFirst("email_verified")?.Value == "true";
                var hasCompleteProfile = !string.IsNullOrEmpty(context.User.FindFirst("given_name")?.Value) && 
                                       !string.IsNullOrEmpty(context.User.FindFirst("family_name")?.Value) && 
                                       !string.IsNullOrEmpty(context.User.FindFirst("email")?.Value);
                var termsAccepted = context.User.FindFirst("terms_accepted")?.Value == "true";
                
                return emailVerified && hasCompleteProfile && termsAccepted;
            });
        });
    }
}
