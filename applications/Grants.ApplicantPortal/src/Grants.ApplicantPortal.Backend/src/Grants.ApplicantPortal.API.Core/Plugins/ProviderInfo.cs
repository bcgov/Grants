namespace Grants.ApplicantPortal.API.Core.Plugins;

/// <summary>
/// Represents a provider available for a plugin
/// </summary>
public record ProviderInfo(
    string Id,
    string Name
);
