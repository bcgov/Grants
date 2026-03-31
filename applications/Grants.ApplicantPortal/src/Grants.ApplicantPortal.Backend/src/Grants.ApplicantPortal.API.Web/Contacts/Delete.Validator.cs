using FluentValidation;

namespace Grants.ApplicantPortal.API.Web.Contacts;

/// <summary>
/// See: https://fast-endpoints.com/docs/validation
/// </summary>
public class DeleteContactValidator : Validator<DeleteContactRequest>
{
  public DeleteContactValidator()
  {
    RuleFor(x => x.ContactId)
      .NotEmpty()
      .WithMessage("ContactId is required.");

    RuleFor(x => x.ApplicantId)
      .NotEmpty()
      .WithMessage("ApplicantId is required.");

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
