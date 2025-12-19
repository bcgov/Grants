using Grants.ApplicantPortal.API.Plugins;

namespace Grants.ApplicantPortal.API.Web.System;

/// <summary>
/// Lists all available plugins with their configuration and features
/// Useful for debugging, API discovery, and system administration
/// </summary>
public class ListPlugins : EndpointWithoutRequest<ListPluginsResponse>
{
    private readonly ILogger<ListPlugins> _logger;

    public ListPlugins(ILogger<ListPlugins> logger)
    {
        _logger = logger;
    }

    public override void Configure()
    {
        Get("/System/plugins");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "List available enabled plugins with their features";
            s.Description = "Returns a list of enabled plugins with their IDs, descriptions, and configured features.";
            s.Responses[200] = "List of enabled plugins with their features";
        });
        
        // Add tags for better Swagger organization
        Tags("System", "Plugins");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        const bool enabledOnly = true; // Backend flag - only return enabled plugins
        
        _logger.LogInformation("Listing enabled plugins only");
        
        var configuredPlugins = PluginRegistry.GetConfiguredPlugins(enabledOnly).ToList();
        
        var plugins = configuredPlugins
            .Select(p => new PluginInfoDto(
                p.PluginId, 
                p.Description,
                // Use configured features (from app settings) as the authoritative source
                p.Configuration?.Features ?? new List<string>()))
            .ToList();

        _logger.LogInformation("Returning {PluginCount} enabled plugins", plugins.Count);
        
        Response = new ListPluginsResponse(plugins);
        await Task.CompletedTask;
    }
}

/// <summary>
/// Response containing list of available plugins with their configurations and features
/// </summary>
public record ListPluginsResponse(IReadOnlyList<PluginInfoDto> Plugins);

/// <summary>
/// Plugin information for API responses including ID, name and features
/// </summary>
public record PluginInfoDto(
    string PluginId, 
    string Description,
    List<string> Features);
