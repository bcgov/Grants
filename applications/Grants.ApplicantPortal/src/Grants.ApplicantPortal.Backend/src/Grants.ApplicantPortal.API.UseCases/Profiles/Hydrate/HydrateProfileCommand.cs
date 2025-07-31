using Grants.ApplicantPortal.API.Core.Plugins;

namespace Grants.ApplicantPortal.API.UseCases.Profiles.Hydrate;

/// <summary>
/// Command to hydrate profile data using plugins and cache the results
/// </summary>
public record HydrateProfileCommand(
    Guid ProfileId,
    string PluginId,
    string Provider,
    string Key,
    Dictionary<string, object>? AdditionalData = null
) : ICommand<Result<ProfileData>>;
