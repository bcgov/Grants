using FluentValidation;
using Grants.ApplicantPortal.API.Plugins;

namespace Grants.ApplicantPortal.API.Web.Addresses;

/// <summary>
/// Validator for RetrieveAddressTypesRequest
/// </summary>
public class RetrieveAddressTypesValidator : Validator<RetrieveAddressTypesRequest>
{
  public RetrieveAddressTypesValidator()
  {
    RuleFor(x => x.PluginId)
      .NotEmpty()
      .WithMessage("PluginId is required")
      .Must(PluginRegistry.IsValidPluginId)
      .When(x => !string.IsNullOrWhiteSpace(x.PluginId))
      .WithMessage(x => $"PluginId '{x.PluginId}' is not a registered plugin. Valid plugins: {string.Join(", ", PluginRegistry.GetAllPluginIds())}");
  }
}
