using FluentValidation;

namespace Grants.ApplicantPortal.API.Web.Profiles;

/// <summary>
/// See: https://fast-endpoints.com/docs/validation
/// </summary>
public class GetProfileValidator : Validator<GetProfileByIdRequest>
{
  public GetProfileValidator()
  {
    RuleFor(x => x.ProfileId)
      .NotEqual(Guid.Empty);
  }
}
