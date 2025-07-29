namespace Grants.ApplicantPortal.API.Web.Auth;

/// <summary>
/// Authentication policy constants for FastEndpoints
/// </summary>
public static class AuthPolicies
{
    #region Basic Authentication Policies
    /// <summary>
    /// Default policy requiring authenticated user
    /// </summary>
    public const string RequireAuthenticatedUser = "RequireAuthenticatedUser";

    /// <summary>
    /// Policy requiring specific realm role from Keycloak
    /// </summary>
    public const string RequireRealmRole = "RequireRealmRole";
    #endregion

    #region Role-Based Policies
    /// <summary>
    /// Policy requiring admin role
    /// </summary>
    public const string AdminOnly = "AdminOnly";

    /// <summary>
    /// Policy requiring user or admin role
    /// </summary>
    public const string UserOrAdmin = "UserOrAdmin";

    /// <summary>
    /// Policy requiring system administrator role (highest level)
    /// </summary>
    public const string SystemAdmin = "SystemAdmin";

    /// <summary>
    /// Policy for program managers who can manage grants programs
    /// </summary>
    public const string ProgramManager = "ProgramManager";

    /// <summary>
    /// Policy for grant officers who can review and process applications
    /// </summary>
    public const string GrantOfficer = "GrantOfficer";
    #endregion

    #region Resource-Based Policies
    /// <summary>
    /// Policy allowing profile reading access
    /// </summary>
    public const string CanReadProfiles = "CanReadProfiles";

    /// <summary>
    /// Policy allowing profile management access
    /// </summary>
    public const string CanManageProfiles = "CanManageProfiles";

    /// <summary>
    /// Policy allowing application submission
    /// </summary>
    public const string CanSubmitApplications = "CanSubmitApplications";

    /// <summary>
    /// Policy allowing application review
    /// </summary>
    public const string CanReviewApplications = "CanReviewApplications";

    /// <summary>
    /// Policy allowing system management
    /// </summary>
    public const string CanManageSystem = "CanManageSystem";
    #endregion

    #region Business Logic Policies
    /// <summary>
    /// Policy requiring verified email address
    /// </summary>
    public const string RequireVerifiedEmail = "RequireVerifiedEmail";

    /// <summary>
    /// Policy requiring complete user profile
    /// </summary>
    public const string RequireCompleteProfile = "RequireCompleteProfile";

    /// <summary>
    /// Policy requiring terms of service acceptance
    /// </summary>
    public const string RequireTermsAcceptance = "RequireTermsAcceptance";

    /// <summary>
    /// Policy requiring fully verified user (email + profile + terms)
    /// </summary>
    public const string FullyVerifiedUser = "FullyVerifiedUser";
    #endregion
}
