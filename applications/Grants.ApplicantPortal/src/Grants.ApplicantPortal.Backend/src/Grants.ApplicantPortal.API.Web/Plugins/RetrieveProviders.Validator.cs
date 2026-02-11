using FluentValidation;
using Grants.ApplicantPortal.API.Plugins;

namespace Grants.ApplicantPortal.API.Web.Plugins;

/// <summary>
/// Validator for RetrieveProvidersRequest
/// </summary>
public class RetrieveProvidersValidator : Validator<RetrieveProvidersRequest>
{
  public RetrieveProvidersValidator()
  {
    RuleFor(x => x.PluginId)
      .NotEmpty()
      .WithMessage("PluginId is required")
      .Must(PluginRegistry.IsValidPluginId)
      .When(x => !string.IsNullOrWhiteSpace(x.PluginId))
      .WithMessage(x => $"PluginId '{x.PluginId}' is not a registered plugin. Valid plugins: {string.Join(", ", PluginRegistry.GetAllPluginIds())}");
  }
}
