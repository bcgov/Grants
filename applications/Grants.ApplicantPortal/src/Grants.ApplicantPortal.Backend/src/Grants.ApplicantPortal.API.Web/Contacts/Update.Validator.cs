using Grants.ApplicantPortal.API.Infrastructure.Data.Config;
using FluentValidation;

namespace Grants.ApplicantPortal.API.Web.Contacts;

/// <summary>
/// See: https://fast-endpoints.com/docs/validation
/// </summary>
public class UpdateContactValidator : Validator<UpdateContactRequest>
{
  public UpdateContactValidator()
  {
    RuleFor(x => x.Name)
      .NotEmpty()
      .WithMessage("Contact name is required.")
      .MinimumLength(2)
      .MaximumLength(DataSchemaConstants.DEFAULT_NAME_LENGTH);

    RuleFor(x => x.Type)
      .NotEmpty()
      .WithMessage("Contact type is required.")
      .MaximumLength(100);

    RuleFor(x => x.Email)
      .EmailAddress()
      .When(x => !string.IsNullOrEmpty(x.Email))
      .WithMessage("Email must be a valid email address.");

    RuleFor(x => x.PhoneNumber)
      .Matches(@"^[\+]?[0-9\-\.\(\)\s]*$")
      .When(x => !string.IsNullOrEmpty(x.PhoneNumber))
      .WithMessage("Phone number format is invalid.");

    RuleFor(x => x.Title)
      .MaximumLength(200)
      .When(x => !string.IsNullOrEmpty(x.Title));

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
