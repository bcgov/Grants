using FluentValidation;

namespace Grants.ApplicantPortal.API.Web.Organizations;

public class UpdateOrganizationValidator : Validator<UpdateOrganizationRequest>
{
  public UpdateOrganizationValidator()
  {
    RuleFor(x => x.Name)
      .NotEmpty().WithMessage("Organization name is required")
      .MaximumLength(200).WithMessage("Organization name must not exceed 200 characters");

    RuleFor(x => x.OrganizationType)
      .NotEmpty().WithMessage("Organization type is required")
      .MaximumLength(100).WithMessage("Organization type must not exceed 100 characters");

    RuleFor(x => x.OrganizationNumber)
      .NotEmpty().WithMessage("Organization number is required")
      .MaximumLength(50).WithMessage("Organization number must not exceed 50 characters");

    RuleFor(x => x.Status)
      .NotEmpty().WithMessage("Organization status is required")
      .MaximumLength(50).WithMessage("Organization status must not exceed 50 characters");

    RuleFor(x => x.LegalName)
      .MaximumLength(200).WithMessage("Legal name must not exceed 200 characters")
      .When(x => !string.IsNullOrEmpty(x.LegalName));

    RuleFor(x => x.DoingBusinessAs)
      .MaximumLength(200).WithMessage("Doing business as name must not exceed 200 characters")
      .When(x => !string.IsNullOrEmpty(x.DoingBusinessAs));

    RuleFor(x => x.Ein)
      .MaximumLength(20).WithMessage("EIN must not exceed 20 characters")
      .When(x => !string.IsNullOrEmpty(x.Ein));

    RuleFor(x => x.Founded)
      .GreaterThan(1800).WithMessage("Founded year must be after 1800")
      .LessThanOrEqualTo(DateTime.Now.Year).WithMessage("Founded year cannot be in the future")
      .When(x => x.Founded.HasValue);

    RuleFor(x => x.FiscalMonth)
      .MaximumLength(20).WithMessage("Fiscal month must not exceed 20 characters")
      .When(x => !string.IsNullOrEmpty(x.FiscalMonth));

    RuleFor(x => x.FiscalDay)
      .InclusiveBetween(1, 31).WithMessage("Fiscal day must be between 1 and 31")
      .When(x => x.FiscalDay.HasValue);

    RuleFor(x => x.Mission)
      .MaximumLength(1000).WithMessage("Mission statement must not exceed 1000 characters")
      .When(x => !string.IsNullOrEmpty(x.Mission));

    RuleFor(x => x.ServiceAreas)
      .Must(areas => areas == null || areas.Length <= 50)
      .WithMessage("Cannot have more than 50 service areas")
      .When(x => x.ServiceAreas != null);

    RuleFor(x => x.ServiceAreas)
      .Must(areas => areas == null || areas.All(area => !string.IsNullOrWhiteSpace(area) && area.Length <= 100))
      .WithMessage("Each service area must be non-empty and not exceed 100 characters")
      .When(x => x.ServiceAreas != null);

    RuleFor(x => x.OrganizationId)
      .NotEmpty().WithMessage("Organization ID is required");

    RuleFor(x => x.ProfileId)
      .NotEmpty().WithMessage("Profile ID is required");

    RuleFor(x => x.PluginId)
      .NotEmpty().WithMessage("Plugin ID is required")
      .MaximumLength(50).WithMessage("Plugin ID must not exceed 50 characters");

    RuleFor(x => x.Provider)
      .NotEmpty().WithMessage("Provider is required")
      .MaximumLength(50).WithMessage("Provider must not exceed 50 characters");
  }
}
