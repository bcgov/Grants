using FluentValidation;

namespace Grants.ApplicantPortal.API.Web.Addresses;

/// <summary>
/// See: https://fast-endpoints.com/docs/validation
/// </summary>
public class CreateAddressValidator : Validator<CreateAddressRequest>
{
  public CreateAddressValidator()
  {
    RuleFor(x => x.AddressType)
      .NotEmpty()
      .WithMessage("Address type is required.")
      .MaximumLength(50);

    RuleFor(x => x.Street)
      .NotEmpty()
      .WithMessage("Street is required.")
      .MaximumLength(200);

    RuleFor(x => x.Street2)
      .MaximumLength(200)
      .When(x => !string.IsNullOrEmpty(x.Street2));

    RuleFor(x => x.Unit)
      .MaximumLength(50)
      .When(x => !string.IsNullOrEmpty(x.Unit));

    RuleFor(x => x.City)
      .NotEmpty()
      .WithMessage("City is required.")
      .MaximumLength(100);

    RuleFor(x => x.Province)
      .NotEmpty()
      .WithMessage("Province is required.")
      .MaximumLength(50);

    RuleFor(x => x.PostalCode)
      .NotEmpty()
      .WithMessage("Postal code is required.")
      .MaximumLength(20);

    RuleFor(x => x.Country)
      .MaximumLength(100)
      .When(x => !string.IsNullOrEmpty(x.Country));

    RuleFor(x => x.ApplicantId)
      .NotEmpty();

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
