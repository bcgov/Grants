namespace Grants.ApplicantPortal.API.Core.Plugins;

/// <summary>
/// Represents a selectable contact role option provided by a plugin
/// </summary>
public record ContactRoleOption(
    string Key,
    string Label
);
