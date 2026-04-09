using Grants.ApplicantPortal.API.Core.Plugins;
using Grants.ApplicantPortal.API.Plugins;
using Grants.ApplicantPortal.API.Web.Auth;
using Grants.ApplicantPortal.API.Web.Profiles;

namespace Grants.ApplicantPortal.API.Web.Plugins;

/// <summary>
/// Retrieves the available providers for a given plugin.
/// Requires authentication.
/// </summary>
public class RetrieveProviders(ILogger<RetrieveProviders> logger, IProfilePluginFactory pluginFactory, IProfileService profileService)
  : Endpoint<RetrieveProvidersRequest, RetrieveProvidersResponse>
{
  public override void Configure()
  {
    Get(RetrieveProvidersRequest.Route);
    Policies(AuthPolicies.RequireAuthenticatedUser);
    Summary(s =>
    {
      s.Summary = "Retrieve providers for a plugin";
      s.Description = "Returns the list of available providers for the specified plugin. Providers are defined by each plugin and represent the data sources or programs it supports.";
      s.Responses[200] = "Providers retrieved successfully (may return an empty list)";
      s.Responses[401] = "Unauthorized - valid JWT token required";
      s.Responses[400] = "Invalid request or plugin validation failed";
      s.Responses[404] = "Plugin not found or not enabled";
      s.Responses[502] = "Unable to retrieve providers from the upstream service";
    });

    Tags("Plugins");
  }

  public override async Task HandleAsync(RetrieveProvidersRequest request, CancellationToken ct)
  {
    var configuredPlugin = PluginRegistry.GetConfiguredPlugin(request.PluginId);

    if (configuredPlugin is null)
    {
      logger.LogWarning("Plugin '{PluginId}' not found or not enabled", request.PluginId);
      await SendNotFoundAsync(ct);
      return;
    }

    var plugin = pluginFactory.GetPlugin(request.PluginId);

    if (plugin is null)
    {
      logger.LogWarning("Plugin '{PluginId}' could not be resolved", request.PluginId);
      await SendNotFoundAsync(ct);
      return;
    }

    try
    {
      var profile = await profileService.GetOrCreateProfileAsync(HttpContext.User, ct);
      var subject = HttpContext.User.GetSubject() ?? string.Empty;

      var providers = await plugin.GetProvidersAsync(profile.Id, subject, ct);

      logger.LogInformation("Returning {ProviderCount} providers for plugin '{PluginId}'",
        providers.Count, request.PluginId);

      Response = new RetrieveProvidersResponse(
        request.PluginId,
        [.. providers.Select(p => new ProviderDto(p.Id, p.Name, p.Metadata))]);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed to retrieve providers for plugin '{PluginId}'", request.PluginId);

      await SendResultAsync(TypedResults.Problem(
        detail: $"Unable to retrieve providers for plugin '{request.PluginId}'. The upstream service may be unavailable.",
        statusCode: StatusCodes.Status502BadGateway,
        title: "Bad Gateway",
        type: "https://tools.ietf.org/html/rfc7231#section-6.6.3"));
    }
  }
}
