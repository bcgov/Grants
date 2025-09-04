using Grants.ApplicantPortal.API.Plugins;

namespace Grants.ApplicantPortal.API.Web.System;

/// <summary>
/// Lists supported features for a specific plugin or all plugins
/// </summary>
public class ListPluginFeatures : EndpointWithoutRequest<ListPluginFeaturesResponse>
{
    public override void Configure()
    {
        Get("/System/plugins/features");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "List supported features for plugins";
            s.Description = "Returns a detailed list of supported provider/key combinations for each plugin";
            s.Responses[200] = "List of supported features by plugin";
        });
        
        Tags("System", "Plugins");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var pluginFeatures = PluginRegistry.GetAllPlugins()
            .Select(p => new PluginFeaturesDto(
                p.PluginId,
                p.Description,
                p.SupportedFeatures.GroupBy(f => f.Provider)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(f => new FeatureDto(f.Key, f.Description)).ToList()
                    )
            ))
            .OrderBy(p => p.PluginId)
            .ToList();

        var allProviders = PluginRegistry.GetAllSupportedProviders();

        Response = new ListPluginFeaturesResponse(pluginFeatures, allProviders);
        await Task.CompletedTask;
    }
}

/// <summary>
/// Response containing detailed plugin features information
/// </summary>
public record ListPluginFeaturesResponse(
    IReadOnlyList<PluginFeaturesDto> PluginFeatures,
    IReadOnlyList<string> AllSupportedProviders);

/// <summary>
/// Plugin features information grouped by provider
/// </summary>
public record PluginFeaturesDto(
    string PluginId,
    string Description,
    Dictionary<string, List<FeatureDto>> ProviderFeatures);

/// <summary>
/// Feature information with key and description
/// </summary>
public record FeatureDto(string Key, string Description);
