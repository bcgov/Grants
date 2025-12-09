using FluentValidation;
using Grants.ApplicantPortal.API.Plugins;

namespace Grants.ApplicantPortal.API.Web.Submissions;

/// <summary>
/// Validator for RetrieveSubmissionsRequest
/// </summary>
public class RetrieveSubmissionsValidator : Validator<RetrieveSubmissionsRequest>
{
  public RetrieveSubmissionsValidator()
  {
    RuleFor(x => x.ProfileId)
      .NotEqual(Guid.Empty)
      .WithMessage("ProfileId must be a valid GUID");

    RuleFor(x => x.PluginId)
      .Must(BeValidPluginId)
      .When(x => !string.IsNullOrEmpty(x.PluginId))
      .WithMessage("PluginId must be a non-empty string when provided")
      .Must(BeValidRegisteredPlugin)
      .When(x => !string.IsNullOrWhiteSpace(x.PluginId))
      .WithMessage(x => $"PluginId '{x.PluginId}' is not a registered plugin. Valid plugins: {string.Join(", ", PluginRegistry.GetAllPluginIds())}");

    RuleFor(x => x.Provider)
      .Must(BeValidProvider)
      .When(x => !string.IsNullOrWhiteSpace(x.PluginId))
      .WithMessage("Provider must be a non-empty string when PluginId is provided");

    RuleFor(x => x.Parameters)
      .Must(BeValidDictionary)
      .When(x => x.Parameters != null)
      .WithMessage("Parameters must be a valid dictionary when provided");
  }

  private static bool BeValidPluginId(string? pluginId)
  {
    return !string.IsNullOrWhiteSpace(pluginId);
  }

  private static bool BeValidRegisteredPlugin(string? pluginId)
  {
    return PluginRegistry.IsValidPluginId(pluginId);
  }

  private static bool BeValidProvider(string? provider)
  {
    return !string.IsNullOrWhiteSpace(provider);
  }

  private static bool BeValidDictionary(Dictionary<string, object>? parameters)
  {
    return true;
  }
}
