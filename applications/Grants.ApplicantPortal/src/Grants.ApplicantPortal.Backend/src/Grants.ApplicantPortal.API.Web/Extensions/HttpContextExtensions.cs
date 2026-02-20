using Grants.ApplicantPortal.API.Core.Features.Profiles.ProfileAggregate;

namespace Grants.ApplicantPortal.API.Web.Extensions;

/// <summary>
/// Extension methods for HttpContext to access profile information
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>
    /// Gets the current user's profile from the HttpContext
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>The current user's profile, or null if not resolved</returns>
    public static Profile? GetProfile(this HttpContext context)
    {
        return context.Items["Profile"] as Profile;
    }

    /// <summary>
    /// Gets the current user's profile ID from the HttpContext
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>The current user's profile ID, or null if not resolved</returns>
    public static Guid? GetProfileId(this HttpContext context)
    {
        return context.Items["ProfileId"] as Guid?;
    }

    /// <summary>
    /// Gets the current user's profile ID from the HttpContext, throwing if not found
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>The current user's profile ID</returns>
    /// <exception cref="InvalidOperationException">Thrown when profile ID is not available</exception>
    public static Guid GetRequiredProfileId(this HttpContext context)
    {
        var profileId = context.GetProfileId();
        if (!profileId.HasValue)
        {
            throw new InvalidOperationException("Profile ID not available in current context. Ensure user is authenticated and profile resolution middleware is configured.");
        }
        return profileId.Value;
    }

    /// <summary>
    /// Gets the current user's profile from the HttpContext, throwing if not found
    /// </summary>
    /// <param name="context">The HTTP context</param>
    /// <returns>The current user's profile</returns>
    /// <exception cref="InvalidOperationException">Thrown when profile is not available</exception>
    public static Profile GetRequiredProfile(this HttpContext context)
    {
        var profile = context.GetProfile();
        if (profile == null)
        {
            throw new InvalidOperationException("Profile not available in current context. Ensure user is authenticated and profile resolution middleware is configured.");
        }
        return profile;
    }
}
