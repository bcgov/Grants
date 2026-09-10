using FluentValidation;
using Grants.ApplicantPortal.API.Plugins;

namespace Grants.ApplicantPortal.API.Web.Submissions;

/// <summary>
/// Validator for RetrieveSubmissionFormRequest
/// </summary>
public class RetrieveSubmissionFormValidator : Validator<RetrieveSubmissionFormRequest>
{
  public RetrieveSubmissionFormValidator()
  {
    RuleFor(x => x.PluginId)
      .NotEmpty()
      .WithMessage("PluginId is required.")
      .Must(BeValidRegisteredPlugin)
      .When(x => !string.IsNullOrWhiteSpace(x.PluginId))
      .WithMessage(x => $"PluginId '{x.PluginId}' is not a registered plugin. Valid plugins: {string.Join(", ", PluginRegistry.GetAllPluginIds())}");

    RuleFor(x => x.Provider)
      .NotEmpty()
      .WithMessage("Provider is required.")
      .MaximumLength(50)
      .WithMessage("Provider must not exceed 50 characters");

    RuleFor(x => x.SubmissionId)
      .NotEmpty()
      .WithMessage("SubmissionId is required.");
  }

  private static bool BeValidRegisteredPlugin(string? pluginId)
  {
    return PluginRegistry.IsValidPluginId(pluginId);
  }
}
