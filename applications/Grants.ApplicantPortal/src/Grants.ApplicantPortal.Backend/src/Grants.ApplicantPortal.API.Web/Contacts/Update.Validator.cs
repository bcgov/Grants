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

    RuleFor(x => x.ContactType)
      .NotEmpty()
      .WithMessage("Contact type is required.")
      .MaximumLength(100);

    RuleFor(x => x.Email)
      .EmailAddress()
      .When(x => !string.IsNullOrEmpty(x.Email))
      .WithMessage("Email must be a valid email address.");

    RuleFor(x => x.HomePhoneNumber)
      .Matches(@"^[\+]?[0-9\-\.\(\)\s]*$")
      .When(x => !string.IsNullOrEmpty(x.HomePhoneNumber))
      .WithMessage("Home phone number format is invalid.");

    RuleFor(x => x.MobilePhoneNumber)
      .Matches(@"^[\+]?[0-9\-\.\(\)\s]*$")
      .When(x => !string.IsNullOrEmpty(x.MobilePhoneNumber))
      .WithMessage("Mobile phone number format is invalid.");

    RuleFor(x => x.WorkPhoneNumber)
      .Matches(@"^[\+]?[0-9\-\.\(\)\s]*$")
      .When(x => !string.IsNullOrEmpty(x.WorkPhoneNumber))
      .WithMessage("Work phone number format is invalid.");

    RuleFor(x => x.Title)
      .MaximumLength(200)
      .When(x => !string.IsNullOrEmpty(x.Title));

    RuleFor(x => x.ContactId)
      .NotEmpty()
      .WithMessage("ContactId is required.");

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
