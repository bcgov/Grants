namespace Grants.ApplicantPortal.API.Core.Plugins;

/// <summary>
/// Represents a supported feature (provider/key combination) for a plugin
/// </summary>
public record PluginSupportedFeature(
    string Provider,
    string Key,
    string Description
);
