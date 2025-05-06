using Grants.ApplicantPortal.API.Core.Profiles;
using Grants.ApplicantPortal.API.Core.Profiles.Interfaces;
using Grants.ApplicantPortal.API.Core.Profiles.ProfileAggregate;
using Microsoft.Extensions.Caching.Hybrid;

namespace Grants.ApplicantPortal.API.UseCases.Profiles.Get;

/// <summary>
/// Retrieve the Applicant Profile from either the cache or populate from profile contributors.
/// </summary>
/// <param name="repository"></param>
/// <param name="hybridCache"></param>
/// <param name="populateProfileService"></param>
public class GetProfileHandler(IReadRepository<Profile> repository,
  HybridCache hybridCache,
  IPopulateProfileService populateProfileService)
  : IQueryHandler<GetProfileQuery, Result<ProfileDTO>>
{
  public async Task<Result<ProfileDTO>> Handle(GetProfileQuery request, CancellationToken cancellationToken)
  {
    var spec = new ProfileByIdSpec(request.ProfileId);
    var entity = await repository.FirstOrDefaultAsync(spec, cancellationToken);
    if (entity == null) return Result.NotFound();

    var prefix = "Profile";

    var entryOptions = new HybridCacheEntryOptions
    {
      Expiration = TimeSpan.FromSeconds(10),
      LocalCacheExpiration = TimeSpan.FromSeconds(10)
    };

    var profile = await hybridCache.GetOrCreateAsync(
    $"{prefix}-{request.ProfileId}", // Unique key to the cache entry
          async cancel => await populateProfileService.PopulateProfile(request.ProfileId, cancellationToken),
          entryOptions,
          cancellationToken: cancellationToken
      );

    return new ProfileDTO(entity.Id, profile);
  }
}
