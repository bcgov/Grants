using Grants.ApplicantPortal.API.Web.Auth;

namespace Grants.ApplicantPortal.API.Web.Auth;

/// <summary>
/// Authentication example endpoint showing how to work with user claims and roles
/// </summary>
public class UserInfo : EndpointWithoutRequest<UserInfoResponse>
{
    public override void Configure()
    {
        Get("/Auth/userinfo");
        Policies(AuthPolicies.RequireAuthenticatedUser);
        Summary(s =>
        {
            s.Summary = "Get current user information";
            s.Description = "Returns current user's information from JWT token claims";
            s.Responses[200] = "User information retrieved successfully";
            s.Responses[401] = "Unauthorized - valid JWT token required";
        });
        
        Tags("Authentication", "User");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var user = HttpContext.User;
        
        Response = new UserInfoResponse(
            user.GetSubject() ?? "Unknown",
            user.GetPreferredUsername() ?? "Unknown",
            user.GetEmail(),
            user.GetFullName(),
            user.GetGivenName(),
            user.GetFamilyName(),
            user.GetRealmRoles().ToArray(),
            user.IsInRole("admin"),
            user.HasRealmRole("user"),
            DateTime.UtcNow
        );

        await Task.CompletedTask;
    }
}

/// <summary>
/// User information response containing claims data
/// </summary>
public record UserInfoResponse(
    string UserId,
    string Username,
    string? Email,
    string? FullName,
    string? FirstName,
    string? LastName,
    string[] RealmRoles,
    bool IsAdmin,
    bool IsUser,
    DateTime RequestedAt
);
