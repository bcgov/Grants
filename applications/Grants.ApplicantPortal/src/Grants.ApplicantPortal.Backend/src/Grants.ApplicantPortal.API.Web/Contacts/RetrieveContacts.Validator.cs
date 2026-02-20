using FluentValidation;
using Grants.ApplicantPortal.API.Plugins;

namespace Grants.ApplicantPortal.API.Web.Contacts;

/// <summary>
/// Validator for RetrieveContactsRequest
/// </summary>
public class RetrieveContactsValidator : Validator<RetrieveContactsRequest>
{
  public RetrieveContactsValidator()
  {
    RuleFor(x => x.PluginId)
      .NotEmpty()
      .WithMessage("PluginId is required")
      .Must(PluginRegistry.IsValidPluginId)
      .When(x => !string.IsNullOrWhiteSpace(x.PluginId))
      .WithMessage(x => $"PluginId '{x.PluginId}' is not a registered plugin. Valid plugins: {string.Join(", ", PluginRegistry.GetAllPluginIds())}");

    RuleFor(x => x.Provider)
      .NotEmpty()
      .When(x => !string.IsNullOrWhiteSpace(x.PluginId))
      .WithMessage("Provider must be a non-empty string when PluginId is provided");

    RuleFor(x => x.Parameters)
      .Must(BeValidDictionary)
      .When(x => x.Parameters != null)
      .WithMessage("Parameters must be a valid dictionary when provided");
  }

  private static bool BeValidDictionary(Dictionary<string, object>? parameters)
  {
    return true;
  }
}
