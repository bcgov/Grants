using Grants.ApplicantPortal.API.Plugins;

namespace Grants.ApplicantPortal.API.Web.System;

/// <summary>
/// Lists all available plugins with their configuration and features
/// Useful for debugging, API discovery, and system administration
/// </summary>
public class ListPlugins(ILogger<ListPlugins> logger) : EndpointWithoutRequest<ListPluginsResponse>
{
  public override void Configure()
  {
    Get("/System/plugins");
    AllowAnonymous();
    Summary(s =>
    {
      s.Summary = "List available enabled plugins with their features and providers";
      s.Description = "Returns a list of enabled plugins with their IDs, descriptions, configured features, and associated providers.";
      s.Responses[200] = "List of enabled plugins with their features and providers";
    });

    // Add tags for better Swagger organization
    Tags("System", "Plugins");
  }

  public override async Task HandleAsync(CancellationToken ct)
  {
    const bool enabledOnly = true; // Backend flag - only return enabled plugins

    logger.LogInformation("Listing enabled plugins only");

    var configuredPlugins = PluginRegistry.GetConfiguredPlugins(enabledOnly).ToList();

    var plugins = configuredPlugins
        .Select(p => new PluginInfoDto(
            p.PluginId,
            p.Description,
            // Use configured features (from app settings) as the authoritative source
            p.Configuration?.Features ?? [],
            // Use configured providers (from app settings) as the authoritative source
            p.Configuration?.Providers ?? []))
        .ToList();

    logger.LogInformation("Returning {PluginCount} enabled plugins", plugins.Count);

    Response = new ListPluginsResponse(plugins);
    await Task.CompletedTask;
  }
}

/// <summary>
/// Response containing list of available plugins with their configurations and features
/// </summary>
public record ListPluginsResponse(IReadOnlyList<PluginInfoDto> Plugins);

/// <summary>
/// Plugin information for API responses including ID, name, features and providers
/// </summary>
public record PluginInfoDto(
    string PluginId,
    string Description,
    List<string> Features,
    List<string> Providers);
