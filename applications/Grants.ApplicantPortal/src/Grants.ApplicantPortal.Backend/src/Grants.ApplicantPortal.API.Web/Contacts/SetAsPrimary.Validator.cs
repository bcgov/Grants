using FluentValidation;

namespace Grants.ApplicantPortal.API.Web.Contacts;

/// <summary>
/// See: https://fast-endpoints.com/docs/validation
/// </summary>
public class SetAsPrimaryContactValidator : Validator<SetAsPrimaryContactRequest>
{
  public SetAsPrimaryContactValidator()
  {
    RuleFor(x => x.ContactId)
      .NotEmpty()
      .WithMessage("ContactId is required.");

    RuleFor(x => x.ProfileId)
      .NotEmpty()
      .WithMessage("ProfileId is required.");

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
