using FluentValidation;

namespace Grants.ApplicantPortal.API.Web.Organizations;

public class UpdateOrganizationValidator : Validator<UpdateOrganizationRequest>
{
  public UpdateOrganizationValidator()
  {
    RuleFor(x => x.Name)
      .NotEmpty().WithMessage("Organization name is required")
      .MaximumLength(250).WithMessage("Organization name must not exceed 200 characters");

    RuleFor(x => x.OrganizationType)
      .NotEmpty().WithMessage("Organization type is required")
      .MaximumLength(100).WithMessage("Organization type must not exceed 100 characters");

    RuleFor(x => x.OrganizationNumber)
      .NotEmpty().WithMessage("Organization number is required")
      .MaximumLength(100).WithMessage("Organization number must not exceed 50 characters");

    RuleFor(x => x.Status)
      .NotEmpty().WithMessage("Organization status is required")
      .MaximumLength(100).WithMessage("Organization status must not exceed 50 characters");

    RuleFor(x => x.NonRegOrgName)
      .MaximumLength(200).WithMessage("Legal name must not exceed 200 characters")
      .When(x => !string.IsNullOrEmpty(x.NonRegOrgName));

    RuleFor(x => x.FiscalMonth)
      .MaximumLength(20).WithMessage("Fiscal month must not exceed 20 characters")
      .When(x => !string.IsNullOrEmpty(x.FiscalMonth));

    RuleFor(x => x.FiscalDay)
      .InclusiveBetween(1, 31).WithMessage("Fiscal day must be between 1 and 31")
      .When(x => x.FiscalDay.HasValue);

    RuleFor(x => x.OrganizationId)
      .NotEmpty().WithMessage("Organization ID is required");

    RuleFor(x => x.PluginId)
      .NotEmpty().WithMessage("Plugin ID is required")
      .MaximumLength(50).WithMessage("Plugin ID must not exceed 50 characters");

    RuleFor(x => x.Provider)
      .NotEmpty().WithMessage("Provider is required")
      .MaximumLength(50).WithMessage("Provider must not exceed 50 characters");
  }
}
