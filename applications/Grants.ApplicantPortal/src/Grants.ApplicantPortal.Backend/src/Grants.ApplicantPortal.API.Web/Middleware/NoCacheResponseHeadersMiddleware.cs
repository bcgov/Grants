namespace Grants.ApplicantPortal.API.Web.Middleware;

/// <summary>
/// Adds Cache-Control: no-store and Pragma: no-cache to every response so that responses
/// carrying applicant/PII data are never cached by browsers or intermediate proxies.
/// </summary>
public class NoCacheResponseHeadersMiddleware(RequestDelegate next)
{
  public async Task InvokeAsync(HttpContext context)
  {
    context.Response.OnStarting(() =>
    {
      context.Response.Headers.CacheControl = "no-store";
      context.Response.Headers.Pragma = "no-cache";
      return Task.CompletedTask;
    });

    await next(context);
  }
}

public static class NoCacheResponseHeadersMiddlewareExtensions
{
  public static IApplicationBuilder UseNoCacheResponseHeaders(this IApplicationBuilder builder) =>
    builder.UseMiddleware<NoCacheResponseHeadersMiddleware>();
}
