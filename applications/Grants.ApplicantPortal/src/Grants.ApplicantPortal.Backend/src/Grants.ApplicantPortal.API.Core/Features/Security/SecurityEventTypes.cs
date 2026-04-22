namespace Grants.ApplicantPortal.API.Core.Features.Security;

/// <summary>
/// Constants for security event types
/// </summary>
public static class SecurityEventTypes
{
    /// <summary>
    /// User successfully logged in
    /// </summary>
    public const string Login = "Login";

    /// <summary>
    /// User logged out
    /// </summary>
    public const string Logout = "Logout";

    /// <summary>
    /// User login failed
    /// </summary>
    public const string LoginFailed = "LoginFailed";

    /// <summary>
    /// User token was refreshed
    /// </summary>
    public const string TokenRefresh = "TokenRefresh";

    /// <summary>
    /// User session expired
    /// </summary>
    public const string SessionExpired = "SessionExpired";

    /// <summary>
    /// User accessed protected resource
    /// </summary>
    public const string ResourceAccess = "ResourceAccess";

    /// <summary>
    /// Unauthorized access attempt
    /// </summary>
    public const string UnauthorizedAccess = "UnauthorizedAccess";

    /// <summary>
    /// User profile was created
    /// </summary>
    public const string ProfileCreated = "ProfileCreated";

    /// <summary>
    /// Resource ownership validation failed — user attempted to access or modify
    /// a resource (contact, address, organization, or applicantId) that does not
    /// belong to their profile
    /// </summary>
    public const string ResourceOwnershipFailure = "ResourceOwnershipFailure";
}
