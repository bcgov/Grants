using FluentValidation;

namespace Grants.ApplicantPortal.API.Web.Addresses;

/// <summary>
/// See: https://fast-endpoints.com/docs/validation
/// </summary>
public class SetAsPrimaryAddressValidator : Validator<SetAsPrimaryAddressRequest>
{
  public SetAsPrimaryAddressValidator()
  {
    RuleFor(x => x.AddressId)
      .NotEmpty()
      .WithMessage("AddressId is required.");

    RuleFor(x => x.PluginId)
      .NotEmpty()
      .WithMessage("PluginId is required.")
      .MaximumLength(50);

    RuleFor(x => x.Provider)
      .NotEmpty()
      .WithMessage("Provider is required.")
      .MaximumLength(50);
  }
}
