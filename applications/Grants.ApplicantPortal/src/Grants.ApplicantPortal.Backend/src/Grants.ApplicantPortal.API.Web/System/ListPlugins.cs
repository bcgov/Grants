using Grants.ApplicantPortal.API.Plugins;
using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.Web.System;

/// <summary>
/// Lists all available plugins across all features
/// Useful for debugging, API discovery, and system administration
/// </summary>
public class ListPlugins : EndpointWithoutRequest<ListPluginsResponse>
{
    public override void Configure()
    {
        Get("/System/plugins");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "List all available plugins";
            s.Description = "Returns a list of all registered plugins with their IDs, descriptions, and supported features across all system features";
            s.Responses[200] = "List of available plugins with supported features";
        });
        
        // Add tags for better Swagger organization
        Tags("System", "Plugins");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var plugins = PluginRegistry.GetAllPlugins()
            .Select(p => new PluginInfoDto(
                p.PluginId, 
                p.Description,
                p.SupportedFeatures.ToList()))
            .OrderBy(p => p.PluginId)
            .ToList();

        Response = new ListPluginsResponse(plugins);
        await Task.CompletedTask;
    }
}

/// <summary>
/// Response containing list of available plugins
/// </summary>
public record ListPluginsResponse(IReadOnlyList<PluginInfoDto> Plugins);

/// <summary>
/// Plugin information for API responses
/// </summary>
public record PluginInfoDto(
    string PluginId, 
    string Description,
    IReadOnlyList<PluginSupportedFeature> SupportedFeatures);
