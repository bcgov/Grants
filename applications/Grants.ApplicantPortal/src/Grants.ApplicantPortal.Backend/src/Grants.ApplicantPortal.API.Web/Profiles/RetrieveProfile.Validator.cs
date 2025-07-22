using FluentValidation;
using Grants.ApplicantPortal.API.Infrastructure.Plugins;

namespace Grants.ApplicantPortal.API.Web.Profiles;

/// <summary>
/// Validator for RetrieveProfileRequest (RetrieveProfile endpoint)
/// See: https://fast-endpoints.com/docs/validation
/// </summary>
public class RetrieveProfileValidator : Validator<RetrieveProfileRequest>
{
  public RetrieveProfileValidator()
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

    RuleFor(x => x.AdditionalData)
      .Must(BeValidDictionary)
      .When(x => x.AdditionalData != null)
      .WithMessage("AdditionalData must be a valid dictionary when provided");
  }

  private static bool BeValidPluginId(string? pluginId)
  {
    return !string.IsNullOrWhiteSpace(pluginId);
  }

  private static bool BeValidRegisteredPlugin(string? pluginId)
  {
    return PluginRegistry.IsValidPluginId(pluginId);
  }

  private static bool BeValidDictionary(Dictionary<string, object>? additionalData)
  {
    // Since we're using a typed Dictionary, basic validation is sufficient
    // Additional business rules can be added here if needed
    return true;
  }
}
