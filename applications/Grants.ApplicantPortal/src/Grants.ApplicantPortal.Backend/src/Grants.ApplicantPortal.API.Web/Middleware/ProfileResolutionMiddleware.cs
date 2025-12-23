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
  private readonly RequestDelegate _next = next;
  private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory;
  private readonly ILogger<ProfileResolutionMiddleware> _logger = logger;

  public async Task InvokeAsync(HttpContext context)
  {
    // Only resolve profile for authenticated users
    if (context.User.Identity?.IsAuthenticated == true)
    {
      try
      {
        using var scope = _serviceScopeFactory.CreateScope();
        var profileService = scope.ServiceProvider.GetRequiredService<IProfileService>();

        var profile = await profileService.GetOrCreateProfileAsync(context.User);

        // Add profile to request context
        context.Items["Profile"] = profile;
        context.Items["ProfileId"] = profile.Id;

        _logger.LogDebug("Profile resolved for user: {ProfileId}", profile.Id);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Failed to resolve profile for authenticated user");
        // Don't fail the request, but continue without profile in context
      }
    }

    await _next(context);
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
