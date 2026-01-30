using Grants.ApplicantPortal.API.Web.Profiles;

namespace Grants.ApplicantPortal.API.Web.Middleware;

/// <summary>
/// Middleware that resolves the current user's profile and adds it to the request context
/// </summary>
public class ProfileResolutionMiddleware(
    RequestDelegate next,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<ProfileResolutionMiddleware> logger)
{
  public async Task InvokeAsync(HttpContext context)
  {
    // Only resolve profile for authenticated users
    if (context.User.Identity?.IsAuthenticated == true)
    {
      try
      {
        using var scope = serviceScopeFactory.CreateScope();
        var profileService = scope.ServiceProvider.GetRequiredService<IProfileService>();

        var profile = await profileService.GetOrCreateProfileAsync(context.User);

        // Add profile to request context
        context.Items["Profile"] = profile;
        context.Items["ProfileId"] = profile.Id;

        logger.LogDebug("Profile resolved for user: {ProfileId}", profile.Id);
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Failed to resolve profile for authenticated user");
        // Don't fail the request, but continue without profile in context
      }
    }

    await next(context);
  }
}

/// <summary>
/// Extension methods for adding profile resolution middleware
/// </summary>
public static class ProfileResolutionMiddlewareExtensions
{
  /// <summary>
  /// Adds profile resolution middleware to the pipeline
  /// </summary>
  public static IApplicationBuilder UseProfileResolution(this IApplicationBuilder builder)
  {
    return builder.UseMiddleware<ProfileResolutionMiddleware>();
  }
}
