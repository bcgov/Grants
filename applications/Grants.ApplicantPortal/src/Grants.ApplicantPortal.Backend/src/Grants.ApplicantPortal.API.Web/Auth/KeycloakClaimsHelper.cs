using System.Security.Claims;
using System.Text.Json;

namespace Grants.ApplicantPortal.API.Web.Auth;

/// <summary>
/// Helper class for working with Keycloak claims and roles
/// </summary>
public static class KeycloakClaimsHelper
{
    /// <summary>
    /// Keycloak claim types
    /// </summary>
    public static class Claims
    {
        public const string PreferredUsername = "preferred_username";
        public const string Email = "email";
        public const string EmailVerified = "email_verified";
        public const string GivenName = "given_name";
        public const string FamilyName = "family_name";
        public const string Name = "name";
        public const string RealmAccess = "realm_access";
        public const string ResourceAccess = "resource_access";
        public const string Roles = "roles";
        public const string Groups = "groups";
        public const string Subject = "sub";
        public const string SessionState = "session_state";
    }

    /// <summary>
    /// Get all realm roles for the current user
    /// </summary>
    public static IEnumerable<string> GetRealmRoles(this ClaimsPrincipal principal)
    {
        var roles = new List<string>();
        
        // Get roles from standard role claims
        roles.AddRange(principal.FindAll(ClaimTypes.Role).Select(c => c.Value));
        roles.AddRange(principal.FindAll("roles").Select(c => c.Value));
        
        return roles.Distinct();
    }

    /// <summary>
    /// Get resource roles for a specific client/resource
    /// </summary>
    public static IEnumerable<string> GetResourceRoles(this ClaimsPrincipal principal, string resourceName)
    {
        var resourceRoles = principal.FindAll("resource_role")
            .Select(c => c.Value)
            .Where(r => r.StartsWith($"{resourceName}:"))
            .Select(r => r.Substring(resourceName.Length + 1));
            
        return resourceRoles;
    }

    /// <summary>
    /// Check if user has a specific realm role
    /// </summary>
    public static bool HasRealmRole(this ClaimsPrincipal principal, string role)
    {
        return principal.GetRealmRoles().Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Check if user has a specific resource role
    /// </summary>
    public static bool HasResourceRole(this ClaimsPrincipal principal, string resourceName, string role)
    {
        return principal.GetResourceRoles(resourceName).Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Get user's preferred username from Keycloak
    /// </summary>
    public static string? GetPreferredUsername(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(Claims.PreferredUsername)?.Value;
    }

    /// <summary>
    /// Get user's email from Keycloak
    /// </summary>
    public static string? GetEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(Claims.Email)?.Value;
    }

    /// <summary>
    /// Get user's full name from Keycloak
    /// </summary>
    public static string? GetFullName(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(Claims.Name)?.Value;
    }

    /// <summary>
    /// Get user's first name from Keycloak
    /// </summary>
    public static string? GetGivenName(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(Claims.GivenName)?.Value;
    }

    /// <summary>
    /// Get user's last name from Keycloak
    /// </summary>
    public static string? GetFamilyName(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(Claims.FamilyName)?.Value;
    }

    /// <summary>
    /// Get user's subject (unique identifier) from Keycloak
    /// </summary>
    public static string? GetSubject(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(Claims.Subject)?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
